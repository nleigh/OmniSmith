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

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "vgmstream-cli",
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
        catch (System.ComponentModel.Win32Exception)
        {
            throw new System.ComponentModel.Win32Exception("The vgmstream-cli tool was not found. Please ensure it is installed and available in your system PATH.");
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
