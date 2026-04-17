using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OmniSmith.Domains.Guitar.Services;

public class PsarcReader
{
    // Rocksmith 2014 Exact Cryptographic Constants
    private static readonly byte[] ARC_KEY = Convert.FromHexString("C53DB23870A1A2F71CAE64061FDD0E1157309DC85204D4C5BFDF25090DF2572C");
    private static readonly byte[] ARC_IV = Convert.FromHexString("E915AA018FEF71FC508132E4BB4CEB42");

    public static byte[] DecryptTOC(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = ARC_KEY;
        aes.IV = ARC_IV;
        aes.Mode = CipherMode.CFB;
        aes.FeedbackSize = 128;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

        using var output = new MemoryStream();
        cs.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Reads a PSARC file and returns all contained entries as filename → byte[] pairs.
    /// </summary>
    public static Dictionary<string, byte[]> ExtractAll(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs);

        // 1. Read header (32 bytes total)
        byte[] magic = br.ReadBytes(4);
        if (Encoding.ASCII.GetString(magic) != "PSAR")
            throw new InvalidDataException($"Not a PSARC file: {filePath}");

        uint versionMajor = ReadUInt16BE(br);
        uint versionMinor = ReadUInt16BE(br);
        string compressionType = Encoding.ASCII.GetString(br.ReadBytes(4)); // "zlib" or "lzma"
        uint tocLength = ReadUInt32BE(br);      // Total TOC size (including header)
        uint tocEntrySize = ReadUInt32BE(br);    // Should be 30
        uint numEntries = ReadUInt32BE(br);
        uint blockSize = ReadUInt32BE(br);       // Typically 65536
        uint archiveFlags = ReadUInt32BE(br);

        // 2. Read and decrypt TOC entries
        // TOC data starts right after the 32-byte header
        long tocDataStart = fs.Position;
        int tocDataLength = (int)(tocLength - 32); // Subtract header size
        byte[] tocRaw = br.ReadBytes(tocDataLength);

        // Decrypt if archive flags indicate encryption (flag bit 2)
        byte[] tocData;
        if ((archiveFlags & 4) != 0)
        {
            // Only the TOC entries portion is encrypted, not the block sizes
            int entriesSize = (int)(numEntries * tocEntrySize);
            byte[] encryptedEntries = new byte[entriesSize];
            Array.Copy(tocRaw, 0, encryptedEntries, 0, entriesSize);
            byte[] decryptedEntries = DecryptTOC(encryptedEntries);
            tocData = new byte[tocDataLength];
            Array.Copy(decryptedEntries, 0, tocData, 0, entriesSize);
            Array.Copy(tocRaw, entriesSize, tocData, entriesSize, tocDataLength - entriesSize);
        }
        else
        {
            tocData = tocRaw;
        }

        // 3. Parse TOC entries (each is 30 bytes)
        var entries = new List<TocEntry>();
        using (var tocStream = new MemoryStream(tocData))
        using (var tocReader = new BinaryReader(tocStream))
        {
            for (int i = 0; i < numEntries; i++)
            {
                var entry = new TocEntry
                {
                    MD5 = tocReader.ReadBytes(16),
                    ZIndex = ReadUInt32BE(tocReader),
                    Length = ReadUInt40BE(tocReader),
                    Offset = ReadUInt40BE(tocReader)
                };
                entries.Add(entry);
            }

            // 4. Read block size table
            // Calculate how many blocks we need based on entries
            int totalBlocks = 0;
            foreach (var entry in entries)
            {
                if (entry.Length == 0) continue;
                int numBlocks = (int)((entry.Length + blockSize - 1) / blockSize);
                totalBlocks = Math.Max(totalBlocks, (int)entry.ZIndex + numBlocks);
            }

            // Block sizes are stored as 2-byte BE values
            var blockSizes = new List<uint>();
            for (int i = 0; i < totalBlocks; i++)
            {
                if (tocStream.Position + 2 <= tocStream.Length)
                {
                    blockSizes.Add(ReadUInt16BE(tocReader));
                }
            }

            // 5. Extract files
            var result = new Dictionary<string, byte[]>();

            // Entry 0 is always the manifest (list of filenames, one per line)
            byte[] manifestData = ExtractEntry(fs, entries[0], blockSizes, blockSize);
            string[] filenames = Encoding.ASCII.GetString(manifestData)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim('\r'))
                .ToArray();

            // Extract remaining entries (entry indices 1..N map to filenames 0..N-1)
            for (int i = 1; i < entries.Count && i - 1 < filenames.Length; i++)
            {
                string name = filenames[i - 1];
                try
                {
                    byte[] data = ExtractEntry(fs, entries[i], blockSizes, blockSize);
                    result[name] = data;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PSARC: Failed to extract '{name}': {ex.Message}");
                }
            }

            return result;
        }
    }

    private static byte[] ExtractEntry(FileStream fs, TocEntry entry, List<uint> blockSizes, uint defaultBlockSize)
    {
        if (entry.Length == 0)
            return Array.Empty<byte>();

        int numBlocks = (int)((entry.Length + defaultBlockSize - 1) / defaultBlockSize);

        using var output = new MemoryStream();
        fs.Position = (long)entry.Offset;

        for (int b = 0; b < numBlocks; b++)
        {
            int blockIdx = (int)entry.ZIndex + b;
            uint compressedSize = blockIdx < blockSizes.Count ? blockSizes[blockIdx] : 0;

            if (compressedSize == 0)
            {
                // Block is stored uncompressed at full block size
                long remaining = (long)entry.Length - output.Position;
                int readSize = (int)Math.Min(remaining, defaultBlockSize);
                byte[] raw = new byte[readSize];
                fs.Read(raw, 0, readSize);
                output.Write(raw, 0, readSize);
            }
            else
            {
                byte[] compressed = new byte[compressedSize];
                fs.Read(compressed, 0, (int)compressedSize);

                // Check for zlib header (0x78)
                if (compressed.Length > 2 && compressed[0] == 0x78)
                {
                    try
                    {
                        // Skip 2-byte zlib header
                        using var compMs = new MemoryStream(compressed, 2, compressed.Length - 2);
                        using var deflate = new DeflateStream(compMs, CompressionMode.Decompress);
                        deflate.CopyTo(output);
                    }
                    catch
                    {
                        // Fallback: treat as raw data
                        output.Write(compressed, 0, compressed.Length);
                    }
                }
                else
                {
                    // Not zlib compressed, store raw
                    output.Write(compressed, 0, compressed.Length);
                }
            }
        }

        return output.ToArray();
    }

    // Big-endian read helpers
    private static uint ReadUInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        return (uint)((b[0] << 8) | b[1]);
    }

    private static uint ReadUInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
    }

    private static ulong ReadUInt40BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(5);
        return ((ulong)b[0] << 32) | ((ulong)b[1] << 24) | ((ulong)b[2] << 16) | ((ulong)b[3] << 8) | b[4];
    }
}

public class TocEntry
{
    public byte[] MD5 { get; set; } = Array.Empty<byte>();
    public uint ZIndex { get; set; }
    public ulong Length { get; set; }
    public ulong Offset { get; set; }
}
