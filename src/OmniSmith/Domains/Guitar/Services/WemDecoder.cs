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

        // TODO for Local Agent (Ticket 2.3):
        // Initialize a ProcessStartInfo to call "vgmstream-cli.exe"
        // Arguments should be: $"-o \"{tempWavPath}\" \"{tempWemPath}\""
        // Set UseShellExecute = false, CreateNoWindow = true
        // Start the process and await Process.WaitForExitAsync()
        // Delete the temporary .wem file to save disk space
        // Return tempWavPath;

        throw new NotImplementedException("Local Agent: Implement vgmstream-cli Process execution here.");
    }
}
