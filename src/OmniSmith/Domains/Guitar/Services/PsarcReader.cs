using System;
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
        var parsed = ParseHeader(filePath);
        var result = new Dictionary<string, byte[]>();

        using var fs = File.OpenRead(filePath);

        // Extract all entries by name
        for (int i = 1; i < parsed.Entries.Count && i - 1 < parsed.Filenames.Length; i++)
        {
            string name = parsed.Filenames[i - 1];
            try
            {
                byte[] data = ExtractEntry(fs, parsed.Entries[i], parsed.BlockSizes, parsed.BlockSize);
                result[name] = data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PSARC: Failed to extract '{name}': {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts only the manifest and specific entries matching a predicate, avoiding full decompression.
    /// Used by GetMetadata to avoid extracting audio/image data during library scanning.
    /// </summary>
    public static Dictionary<string, byte[]> ExtractByFilter(string filePath, Func<string, bool> filter)
    {
        var parsed = ParseHeader(filePath);
        var result = new Dictionary<string, byte[]>();

        using var fs = File.OpenRead(filePath);

        for (int i = 1; i < parsed.Entries.Count && i - 1 < parsed.Filenames.Length; i++)
        {
            string name = parsed.Filenames[i - 1];
            if (!filter(name)) continue;

            try
            {
                byte[] data = ExtractEntry(fs, parsed.Entries[i], parsed.BlockSizes, parsed.BlockSize);
                result[name] = data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PSARC: Failed to extract '{name}': {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the list of filenames contained in the PSARC without extracting any data.
    /// </summary>
    public static string[] ListEntries(string filePath)
    {
        return ParseHeader(filePath).Filenames;
    }

    private record ParsedPsarc(
        List<TocEntry> Entries,
        List<uint> BlockSizes,
        uint BlockSize,
        string[] Filenames
    );

    private static ParsedPsarc ParseHeader(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs);

        // 1. Read header (32 bytes total)
        byte[] magic = br.ReadBytes(4);
        if (Encoding.ASCII.GetString(magic) != "PSAR")
            throw new InvalidDataException($"Not a PSARC file: {filePath}");

        uint versionMajor = ReadUInt16BE(br);
        uint versionMinor = ReadUInt16BE(br);
        string compressionType = Encoding.ASCII.GetString(br.ReadBytes(4)).TrimEnd('\0');
        uint tocLength = ReadUInt32BE(br);      // Total TOC size (including header)
        uint tocEntrySize = ReadUInt32BE(br);    // Should be 30
        uint numEntries = ReadUInt32BE(br);
        uint blockSize = ReadUInt32BE(br);       // Typically 65536
        uint archiveFlags = ReadUInt32BE(br);

        // Validate compression type  
        if (!compressionType.Equals("zlib", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Unsupported PSARC compression type: '{compressionType}'. Only zlib is supported.");

        // 2. Read and decrypt TOC entries
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
        List<uint> blockSizes;

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
            int totalBlocks = 0;
            foreach (var entry in entries)
            {
                if (entry.Length == 0) continue;
                int numBlocks = (int)((entry.Length + blockSize - 1) / blockSize);
                totalBlocks = Math.Max(totalBlocks, (int)entry.ZIndex + numBlocks);
            }

            blockSizes = new List<uint>();
            for (int i = 0; i < totalBlocks; i++)
            {
                if (tocStream.Position + 2 <= tocStream.Length)
                {
                    blockSizes.Add(ReadUInt16BE(tocReader));
                }
            }
        }

        // 5. Extract manifest (entry 0 is always the filename list)
        byte[] manifestData = ExtractEntry(fs, entries[0], blockSizes, blockSize);
        string[] filenames = Encoding.ASCII.GetString(manifestData)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim('\r'))
            .ToArray();

        return new ParsedPsarc(entries, blockSizes, blockSize, filenames);
    }

    private static byte[] ExtractEntry(Stream fs, TocEntry entry, List<uint> blockSizes, uint defaultBlockSize)
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

                // Use ZLibStream for proper zlib decompression (handles header + Adler-32 trailer)
                if (compressed.Length > 2 && compressed[0] == 0x78)
                {
                    using var compMs = new MemoryStream(compressed);
                    using var zlib = new ZLibStream(compMs, CompressionMode.Decompress);
                    zlib.CopyTo(output);
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
