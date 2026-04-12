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
    public List<float> Beats { get; set; } = new();
    public List<float> Anchors { get; set; } = new();
    
    // Audio engine reference will be populated during Factory Loading
    public string? CachedWavPath { get; set; }

    public GuitarSong()
    {
    }

    public void Update(float audioTimeMs)
    {
        // TODO for Local Agent: 
        // Synchronize ManagedBass playback timing logic here.
    }

    public void Draw(ImDrawListPtr drawList)
    {
        var io = ImGui.GetIO();
        var pos = new System.Numerics.Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
        drawList.AddText(pos, 0xFFFFFFFF, "Loading Guitar Data...");
    }

    public void Dispose()
    {
        // Clean up ManagedBass streams and delete CachedWavPath from disk
        if (!string.IsNullOrEmpty(CachedWavPath) && System.IO.File.Exists(CachedWavPath))
        {
            try { System.IO.File.Delete(CachedWavPath); } catch { }
        }
    }
}
