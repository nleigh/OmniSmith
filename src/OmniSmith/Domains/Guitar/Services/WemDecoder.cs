using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OmniSmith.Domains.Guitar.Services;

public class WemDecoder
{
    /// <summary>
    /// Executes vgmstream-cli to convert Rocksmith Wwise Audio to uncompressed WAV format.
    /// </summary>
    /// <returns>The path to the generated WAV file.</returns>
    public static async Task<string> ConvertWemToWavAsync(byte[] wemData, string cacheDirectory)
    {
        if (!Directory.Exists(cacheDirectory))
            Directory.CreateDirectory(cacheDirectory);

        string tempWemPath = Path.Combine(cacheDirectory, Guid.NewGuid().ToString() + ".wem");
        string tempWavPath = Path.Combine(cacheDirectory, Guid.NewGuid().ToString() + ".wav");

        await File.WriteAllBytesAsync(tempWemPath, wemData);

        // Initialize a ProcessStartInfo to call "vgmstream-cli.exe"
        var tcs = new TaskCompletionSource<bool>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "vgmstream-cli",
                Arguments = $"-o \"{tempWavPath}\" \"{tempWemPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(true);
            process.Dispose();
        };

        process.Start();
        await tcs.Task;

        // Delete the temporary .wem file to save disk space
        if (File.Exists(tempWemPath))
            File.Delete(tempWemPath);

        return tempWavPath;
    }
}
