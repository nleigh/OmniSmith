using System;
using System.Threading.Tasks;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar.Services;

namespace OmniSmith.Core;

public static class SongFactory
{
    public static async Task<IPlayableSong> LoadSongAsync(string filePath)
    {
        string extension = System.IO.Path.GetExtension(filePath).ToLower();

        if (extension == ".mid")
        {
            // PianoSong is currently missing from the codebase or not yet ported.
            // For now, we return null or throw to indicate it needs implementation.
            throw new NotImplementedException("PianoSong domain is not yet implemented in this version of the codebase.");
        }

        if (extension == ".psarc")
        {
            return await Task.Run(() => RocksmithParser.ParsePsarc(filePath));
        }

        throw new NotSupportedException($"Unsupported file extension: {extension}");
    }
}
