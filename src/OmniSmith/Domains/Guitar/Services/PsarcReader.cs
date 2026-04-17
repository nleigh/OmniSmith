using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rocksmith2014.PSARC;
using OmniSmith.Core;

namespace OmniSmith.Domains.Guitar.Services;

public class PsarcReader
{
    /// <summary>
    /// Reads a PSARC file and returns all contained entries as filename → byte[] pairs.
    /// Uses the robust Rocksmith2014.PSARC library for extraction.
    /// </summary>
    public static Dictionary<string, byte[]> ExtractAll(string filePath)
    {
        Logger.Info($"PSARC: Extracting all entries from '{filePath}' using Rocksmith2014.PSARC");
        var result = new Dictionary<string, byte[]>();

        try
        {
            using var psarc = PSARC.OpenFile(filePath);
            foreach (var name in psarc.Manifest)
            {
                try
                {
                    // Rocksmith2014.PSARC returns a Task<MemoryStream> for GetEntryStream
                    using var stream = psarc.GetEntryStream(name).Result;
                    result[name] = stream.ToArray();
                }
                catch (Exception ex)
                {
                    Logger.Error($"PSARC: Failed to extract '{name}'", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"PSARC: Failed to open archive '{filePath}'", ex);
        }

        return result;
    }

    /// <summary>
    /// Extracts specific entries matching a predicate, avoiding full decompression.
    /// </summary>
    public static Dictionary<string, byte[]> ExtractByFilter(string filePath, Func<string, bool> filter)
    {
        var result = new Dictionary<string, byte[]>();

        try
        {
            using var psarc = PSARC.OpenFile(filePath);
            foreach (var name in psarc.Manifest)
            {
                if (!filter(name)) continue;

                try
                {
                    using var stream = psarc.GetEntryStream(name).Result;
                    result[name] = stream.ToArray();
                }
                catch (Exception ex)
                {
                    Logger.Error($"PSARC: Failed to extract filter match '{name}'", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"PSARC: Failed to open archive for filter '{filePath}'", ex);
        }

        return result;
    }

    /// <summary>
    /// Returns the list of filenames contained in the PSARC without extracting any data.
    /// </summary>
    public static string[] ListEntries(string filePath)
    {
        try
        {
            using var psarc = PSARC.OpenFile(filePath);
            // .Manifest is an F# list (string list), convert to array
            return psarc.Manifest.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error($"PSARC: Failed to list entries for '{filePath}'", ex);
            return System.Array.Empty<string>();
        }
    }
}
