using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Syroot.Windows.IO;

namespace OmniSmith.Settings;

public static class MidiPathsManager
{
    public static List<string> MidiPaths { get; private set; } = new()
    {
        KnownFolders.Documents.Path,
        KnownFolders.Downloads.Path,
        KnownFolders.Music.Path,
    };

    public static void LoadValidPaths(List<string> paths)
    {
        foreach (var folderPath in paths)
        {
            if (Directory.Exists(folderPath) && !MidiPaths.Contains(folderPath))
                MidiPaths.Add(folderPath);
        }
    }

    /// <summary>
    /// Safe directory traversal that catches UnauthorizedAccessException and skips restricted folders.
    /// </summary>
    public static IEnumerable<string> SafeEnumerateFiles(string root, CancellationToken token = default)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            if (token.IsCancellationRequested) yield break;

            string currentDir = stack.Pop();
            
            string[]? files = null;
            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }

            if (files != null)
            {
                foreach (var file in files) yield return file;
            }

            string[]? subDirs = null;
            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }

            if (subDirs != null)
            {
                foreach (var dir in subDirs)
                {
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        if (!di.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            stack.Push(dir);
                        }
                    }
                    catch { } // Skip if we can't read attributes
                }
            }
        }
    }
}
