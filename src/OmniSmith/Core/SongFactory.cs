using System;
using System.Threading.Tasks;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar.Services;
using OmniSmith.Domains.Piano;

namespace OmniSmith.Core;

public static class SongFactory
{
    public static async Task<IPlayableSong> LoadSongAsync(string filePath)
    {
        string extension = System.IO.Path.GetExtension(filePath).ToLower();

        if (extension == ".mid")
        {
            return await Task.Run(() => new PianoSong(filePath));
        }

        if (extension == ".psarc")
        {
            return await Task.Run(() => RocksmithParser.ParsePsarc(filePath));
        }

        throw new NotSupportedException($"Unsupported file extension: {extension}");
    }
}
