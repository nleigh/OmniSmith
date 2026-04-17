using System;
using System.IO;
using System.Text;
using Syroot.Windows.IO;

namespace OmniSmith.Core;

public static class Logger
{
    private static string _logPath;
    private static readonly object _lock = new object();
    private const long MaxLogSize = 10 * 1024 * 1024; // 10MB

    static Logger()
    {
        _logPath = Path.Combine(ProgramData.AppDir, "OmniSmith.log");
    }

    public static void Initialize()
    {
        try
        {
            string dir = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Check size and rotate if necessary
            FileInfo fileInfo = new FileInfo(_logPath);
            if (fileInfo.Exists && fileInfo.Length > MaxLogSize)
            {
                File.Move(_logPath, _logPath + ".old", true);
            }

            Log("==================================================");
            Log($"Application Started - Version {ProgramData.ProgramVersion}");
            Log($"OS: {Environment.OSVersion}");
            Log("==================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize logger: {ex.Message}");
        }
    }

    public static void Info(string message) => Log($"[INFO] {message}");
    public static void Warning(string message) => Log($"[WARN] {message}");
    public static void Error(string message, Exception ex = null)
    {
        string msg = $"[ERROR] {message}";
        if (ex != null)
        {
            msg += $"\nException: {ex.Message}\nStack: {ex.StackTrace}";
        }
        Log(msg);
    }

    private static void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] {message}";

        // Always print to console
        Console.WriteLine(formattedMessage);

        // Append to file
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, formattedMessage + Environment.NewLine);
            }
            catch
            {
                // Nowhere to log the logging failure!
            }
        }
    }
}
