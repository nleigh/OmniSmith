using System;
using System.IO;
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
        // Python equivalent: AES.new(ARC_KEY, AES.MODE_CFB, iv=ARC_IV, segment_size=128)
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
    /// Reads a PSARC file, decrypts the header/TOC, and decompresses 
    /// the requested file paths to memory.
    /// </summary>
    public static void ParsePsarc(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs);

        // TODO for Local Agent (Ticket 2.2):
        // 1. Read MAGIC bytes ("PSAR")
        // 2. Read PSARC header (Version, TOC Length, Entry size, etc)
        // 3. Extract the TOC block bytes, and pass to DecryptTOC()
        // 4. Parse the unencrypted TOC struct to locate Uncompressed Size and Block Offsets.
        // 5. Utilize System.IO.Compression.ZLibStream (or equivalent zlib deflater) to extract blocks.
    }
}
