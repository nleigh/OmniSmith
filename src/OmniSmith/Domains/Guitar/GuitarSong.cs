using ImGuiNET;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Core.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
    
    public bool SynthGuitarEnabled { get; set; } = false;
    private HashSet<GuitarNote> _activeNotes = new();
    private float _currentAudioTimeMs;

    public string? CachedWavPath { get; set; }

    public GuitarSong()
    {
    }

    public void Update(float audioTimeMs)
    {
        _currentAudioTimeMs = audioTimeMs;

        if (SynthGuitarEnabled && MidiPlayer.SoundFontEngine != null)
        {
            // Note On
            foreach (var note in Notes.Where(n => n.Time <= audioTimeMs && n.Time > audioTimeMs - 10f))
            {
                int pitch = GetPitch(note.String, note.Fret);
                MidiPlayer.SoundFontEngine.PlayNote(0, pitch, 100);
                _activeNotes.Add(note);
            }

            // Note Off
            var toRemove = _activeNotes.Where(n => n.Time + n.Duration < audioTimeMs).ToList();
            foreach (var note in toRemove)
            {
                int pitch = GetPitch(note.String, note.Fret);
                MidiPlayer.SoundFontEngine.StopNote(0, pitch);
                _activeNotes.Remove(note);
            }
        }
    }

    private int GetPitch(int stringIdx, int fret)
    {
        // Standard Tuning: E2, A2, D3, G3, B3, E4
        int[] basePitches = { 40, 45, 50, 55, 59, 64 };
        if (stringIdx < 0 || stringIdx >= 6) return 60;
        return basePitches[stringIdx] + fret;
    }

    public void Draw(ImDrawListPtr drawList)
    {
        var io = ImGui.GetIO();
        float vanishingPointX = io.DisplaySize.X / 2f;
        float horizonY = io.DisplaySize.Y * 0.2f;
        float hitLineY = io.DisplaySize.Y * 0.9f;
        float fieldOfView = 800f;

        // Colors
        uint colorFretboard = 0x88000000; 
        uint colorHitline = 0xFF00FF00;   
        uint[] stringColors = { 0xFFFF0000, 0xFFFFFF00, 0xFF0000FF, 0xFFFFA500, 0xFF00FF00, 0xFF800080 };

        // 1. Draw Fretboard
        var tl = Project(-300, 5000, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
        var tr = Project(300, 5000, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
        var bl = Project(-300, 0, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
        var br = Project(300, 0, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
        drawList.AddQuadFilled(tl, tr, br, bl, colorFretboard);

        // 2. Draw Strings
        for (int i = 0; i < 6; i++)
        {
            float strX = -250 + (i * 100);
            var p1 = Project(strX, 5000, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
            var p2 = Project(strX, 0, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
            drawList.AddLine(p1, p2, stringColors[i], 2f);
        }

        // 3. Draw Hitline
        drawList.AddLine(bl, br, colorHitline, 3f);

        // 4. Draw Beats
        foreach (var beat in Beats)
        {
            float zDepth = (beat - _currentAudioTimeMs) * 100f;
            if (zDepth >= 0 && zDepth <= 5000)
            {
                var bp1 = Project(-300, zDepth, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
                var bp2 = Project(300, zDepth, vanishingPointX, horizonY, hitLineY, fieldOfView).ScreenPos;
                drawList.AddLine(bp1, bp2, 0xFFFFFFFF, 1f);
            }
        }

        // 5. Draw Notes and Sustain
        foreach (var note in Notes)
        {
            float zStart = (note.Time - _currentAudioTimeMs) * 100f;
            float zEnd = (note.Time + note.Duration - _currentAudioTimeMs) * 100f;

            if (zEnd < 0 || zStart > 5000) continue;

            float strX = -250 + (note.String * 100);
            uint noteColor = stringColors[note.String];

            if (note.Duration > 0)
            {
                var pStart = Project(strX, zStart, vanishingPointX, horizonY, hitLineY, fieldOfView);
                var pEnd = Project(strX, zEnd, vanishingPointX, horizonY, hitLineY, fieldOfView);
                float thickness = 8f * pStart.Scale;
                drawList.AddLine(pStart.ScreenPos, pEnd.ScreenPos, noteColor, thickness);
            }

            var proj = Project(strX, zStart, vanishingPointX, horizonY, hitLineY, fieldOfView);
            if (proj.Scale > 0)
            {
                float radius = 15f * proj.Scale;
                drawList.AddCircleFilled(proj.ScreenPos, radius, noteColor);
                
                if (proj.Scale > 0.5f)
                {
                    // Finger number
                    string fingerText = note.Finger >= 0 ? note.Finger.ToString() : note.Fret.ToString();
                    drawList.AddText(proj.ScreenPos - new Vector2(radius, radius), 0xFFFFFFFF, fingerText);

                    // Technique Label
                    string techLabel = "";
                    if (note.Techniques.HasFlag(NoteTechnique.HammerOn)) techLabel = "H";
                    else if (note.Techniques.HasFlag(NoteTechnique.PullOff)) techLabel = "P";
                    else if (note.Techniques.HasFlag(NoteTechnique.Slide)) techLabel = "S";
                    else if (note.Techniques.HasFlag(NoteTechnique.Bend)) techLabel = "B";

                    if (!string.IsNullOrEmpty(techLabel))
                    {
                        drawList.AddText(proj.ScreenPos + new Vector2(radius, radius), 0xFFFFFF00, techLabel);
                    }
                }
            }
        }

        var pos = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
        drawList.AddText(pos, 0xFFFFFFFF, "Guitar Highway Active");
    }

    public (Vector2 ScreenPos, float Scale) Project(float trackX, float zDepth, float vanishingPointX, float horizonY, float hitLineY, float fieldOfView)
    {
        float scale = fieldOfView / (fieldOfView + Math.Max(0, zDepth));
        float px = vanishingPointX + trackX * scale;
        float py = horizonY + (hitLineY - horizonY) * scale;

        return (new Vector2(px, py), scale);
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(CachedWavPath) && System.IO.File.Exists(CachedWavPath))
        {
            try { System.IO.File.Delete(CachedWavPath); } catch { }
        }
    }
}
