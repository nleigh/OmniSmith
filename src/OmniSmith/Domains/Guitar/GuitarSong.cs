using ImGuiNET;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar.Models;
using System;
using System.Collections.Generic;

namespace OmniSmith.Domains.Guitar;

public class GuitarSong : IPlayableSong
{
    public string Title { get; set; } = "Unknown Title";
    public string Artist { get; set; } = "Unknown Artist";
    public TimeSpan TotalDuration { get; set; }

    public List<GuitarNote> Notes { get; set; } = new();
    public List<GuitarChord> Chords { get; set; } = new();
    
    // Audio engine reference will be populated during Factory Loading
    public string CachedWavPath { get; set; }

    public GuitarSong()
    {
    }

    public void Update(float audioTimeMs)
    {
        // TODO for Local Agent: 
        // Synchronize NAudio playback timing logic here.
    }

    public void Draw(ImDrawListPtr drawList)
    {
        // TODO for Local Agent (Ticket 3.x):
        // Implement 3D Highway Projection here.
    }

    public void Dispose()
    {
        // Clean up NAudio streams and delete CachedWavPath from disk
        if (System.IO.File.Exists(CachedWavPath))
        {
            try { System.IO.File.Delete(CachedWavPath); } catch { }
        }
    }
}
