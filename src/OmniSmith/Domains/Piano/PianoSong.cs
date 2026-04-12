using ImGuiNET;
using OmniSmith.Core.Interfaces;
using OmniSmith.Core.Midi;
using System;
using System.Collections.Generic;
using System.Numerics;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace OmniSmith.Domains.Piano;

public class PianoSong : IPlayableSong
{
    public string Title { get; set; }
    public string Artist { get; set; } = "Unknown Artist";
    public TimeSpan TotalDuration { get; set; }

    public MidiFile MidiFile { get; private set; }
    public List<Note> Notes { get; private set; }
    public List<float> Beats { get; set; } = new();

    private float _currentAudioTimeMs;

    public PianoSong(string filePath)
    {
        MidiFile = MidiFile.Read(filePath);
        Notes = MidiFile.GetNotes();
        Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
        TotalDuration = MidiFile.GetTempoMap().TotalTime;
    }

    public void Update(float audioTimeMs)
    {
        _currentAudioTimeMs = audioTimeMs;
    }

    public void Draw(ImDrawListPtr drawList)
    {
        // Basic placeholder for Piano visualization (falling notes)
        // This will be expanded in later milestones to fully port the legacy renderer
        var io = ImGui.GetIO();
        var pos = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
        drawList.AddText(pos, 0xFFFFFFFF, $"Piano View: {Title}");
    }

    public void Dispose()
    {
    }
}
