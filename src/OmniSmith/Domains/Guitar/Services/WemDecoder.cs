using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace OmniSmith.Domains.Guitar.Services;

public class WemDecoder
{
    private static readonly string ToolDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OmniSmith", "Tools");

    private static readonly string VgmstreamPath = Path.Combine(ToolDir, "vgmstream-cli.exe");

    private const string VGMSTREAM_DOWNLOAD_URL =
        "https://github.com/vgmstream/vgmstream/releases/download/r2083/vgmstream-win64.zip";

    /// <summary>
    /// Ensures vgmstream-cli.exe is available locally, downloading it if necessary.
    /// </summary>
    private static async Task EnsureVgmstreamAsync()
    {
        if (File.Exists(VgmstreamPath))
            return;

        Console.WriteLine("vgmstream-cli not found. Downloading automatically...");

        if (!Directory.Exists(ToolDir))
            Directory.CreateDirectory(ToolDir);

        string tempZip = Path.Combine(ToolDir, "vgmstream-win64.zip");

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("OmniSmith/1.0");
            var bytes = await http.GetByteArrayAsync(VGMSTREAM_DOWNLOAD_URL);
            await File.WriteAllBytesAsync(tempZip, bytes);

            ZipFile.ExtractToDirectory(tempZip, ToolDir, overwriteFiles: true);

            if (!File.Exists(VgmstreamPath))
                throw new FileNotFoundException("vgmstream-cli.exe not found in downloaded archive.");

            Console.WriteLine($"vgmstream-cli installed to {ToolDir}");
        }
        finally
        {
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }
    }

    /// <summary>
    /// Executes vgmstream-cli to convert Rocksmith Wwise Audio to uncompressed WAV format.
    /// Automatically downloads vgmstream-cli if not already present.
    /// </summary>
    /// <returns>The path to the generated WAV file.</returns>
    public static async Task<string> ConvertWemToWavAsync(byte[] wemData, string cacheDirectory)
    {
        if (!Directory.Exists(cacheDirectory))
            Directory.CreateDirectory(cacheDirectory);

        // Ensure the tool is available before attempting conversion
        await EnsureVgmstreamAsync();

        string tempWemPath = Path.Combine(cacheDirectory, Guid.NewGuid().ToString() + ".wem");
        string tempWavPath = Path.Combine(cacheDirectory, Guid.NewGuid().ToString() + ".wav");

        await File.WriteAllBytesAsync(tempWemPath, wemData);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = VgmstreamPath,
                    Arguments = $"-o \"{tempWavPath}\" \"{tempWemPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(tempWavPath))
            {
                throw new InvalidOperationException($"Failed to decode audio. vgmstream-cli exited with code {process.ExitCode}.");
            }
        }
        finally
        {
            // Delete the temporary .wem file to save disk space
            if (File.Exists(tempWemPath))
                File.Delete(tempWemPath);
        }

        return tempWavPath;
    }
}
