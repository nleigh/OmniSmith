using ImGuiNET;
using OmniSmith.Core.Interfaces;
using OmniSmith.Core.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace OmniSmith.Domains.Piano;

public class PianoSong : IPlayableSong
{
    public string Title { get; set; }
    public string Artist { get; set; } = "Unknown Artist";
    public TimeSpan TotalDuration { get; set; }

    public Melanchall.DryWetMidi.Core.MidiFile SongFile { get; private set; }
    public List<Note> Notes { get; private set; }
    public List<float> Beats { get; set; } = new();
 
    private float _currentAudioTimeMs;
 
    public PianoSong(string filePath)
    {
        SongFile = Melanchall.DryWetMidi.Core.MidiFile.Read(filePath);
        var notesCollection = SongFile.GetNotes();
        Notes = notesCollection.ToList();
        Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
        
        if (Notes.Count > 0)
        {
            var maxEndTime = Notes.Max(n => n.EndTime);
            TotalDuration = SongFile.GetTempoMap().ToTimeSpan(maxEndTime);
        }
        else
        {
            TotalDuration = TimeSpan.Zero;
        }
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
