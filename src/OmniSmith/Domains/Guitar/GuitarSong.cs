using ImGuiNET;
using OmniSmith.Core;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Core.Models;
using OmniSmith.Core.Midi;
using OmniSmith.Ui.Helpers;
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
    public float CurrentAudioPositionMs => (float)(_audioFileReader?.CurrentTime.TotalMilliseconds ?? 0);

    public List<GuitarNote> Notes { get; set; } = new();
    public List<GuitarChord> Chords { get; set; } = new();
    public List<float> Beats { get; set; } = new();
    public List<SongSection> Sections { get; set; } = new();
    public List<float> Anchors { get; set; } = new();
    
    public bool SynthGuitarEnabled { get; set; } = false;
    private HashSet<GuitarNote> _activeNotes = new();
    private float _currentAudioTimeMs;
    private long _lastLogTick;
    public string? PsarcPath { get; set; }
    public string? ArrangementXml { get; set; }
    public List<string> AvailableArrangements { get; set; } = new();

    public string? CachedWavPath { get; set; }

    private NAudio.Wave.AudioFileReader? _audioFileReader;
    private NAudio.Wave.WaveOutEvent? _waveOut;
    private bool _isAudioInitialized = false;

    public GuitarSong()
    {
    }

    public void InitAudio()
    {
        if (string.IsNullOrEmpty(CachedWavPath) || !System.IO.File.Exists(CachedWavPath) || _isAudioInitialized)
            return;

        try 
        {
            OmniSmith.Core.Logger.Info($"GuitarSong: Initializing audio from {CachedWavPath}");
            _audioFileReader = new NAudio.Wave.AudioFileReader(CachedWavPath);
            _waveOut = new NAudio.Wave.WaveOutEvent();
            _waveOut.Init(_audioFileReader);
            _isAudioInitialized = true;
            OmniSmith.Core.Logger.Info("GuitarSong: Audio system initialized successfully");
        }
        catch (Exception ex)
        {
            OmniSmith.Core.Logger.Error($"GuitarSong: Failed to initialize audio: {ex.Message}");
        }
    }

    public void Update(float audioTimeMs)
    {
        _currentAudioTimeMs = audioTimeMs;

        if (_waveOut != null && _audioFileReader != null)
        {
            if (MidiPlayer.IsTimerRunning && _waveOut.PlaybackState != NAudio.Wave.PlaybackState.Playing)
            {
                _waveOut.Play();
            }
            else if (!MidiPlayer.IsTimerRunning && _waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                _waveOut.Pause();
            }
            
            // Re-sync if visually out of phase by more than 50ms
            if (MidiPlayer.IsTimerRunning && Math.Abs(_audioFileReader.CurrentTime.TotalMilliseconds - audioTimeMs) > 50)
            {
                _audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(audioTimeMs);
            }

            // Periodic Health Check Log (every 2 seconds)
            if (DateTime.Now.Ticks - _lastLogTick > 2 * TimeSpan.TicksPerSecond)
            {
                _lastLogTick = DateTime.Now.Ticks;
                OmniSmith.Core.Logger.Info($"GuitarSong: Audio Sync - Timer={audioTimeMs:F0}ms, Audio={_audioFileReader.CurrentTime.TotalMilliseconds:F0}ms, State={_waveOut.PlaybackState}");
            }
        }

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

    // SLOPSMITH PORT MATH
    private const float VISIBLE_SECONDS = 8.0f; // Increased for 'Incoming Notes' visibility
    private const float Z_CAM = 2.5f; 
    private const float Z_MAX = 10.0f;
    private const float Y_HITLINE_NORM = 0.92f; 
    private const float Y_HORIZON_NORM = 0.25f; 

    private System.Diagnostics.Stopwatch _interpolationTimer = new();
    private double _lastAudioTimeMs;

    public void Draw(ImDrawListPtr drawList)
    {
        var io = ImGui.GetIO();
        float screenW = io.DisplaySize.X;
        float screenH = io.DisplaySize.Y;

        // HIGH-RESOLUTION INTERPOLATION
        // Sync visual clock with audio clock
        double currentAudioTime = _audioFileReader?.CurrentTime.TotalMilliseconds ?? 0;
        
        bool isPlaying = _waveOut != null && _waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing;

        if (!isPlaying)
        {
            _interpolationTimer.Stop();
            _interpolationTimer.Reset();
            _lastAudioTimeMs = currentAudioTime;
        }
        else if (currentAudioTime != _lastAudioTimeMs)
        {
            _lastAudioTimeMs = currentAudioTime;
            _interpolationTimer.Restart();
        }

        float displayTime = (float)currentAudioTime;
        if (isPlaying)
        {
            // Clamp interpolation to 100ms to avoid runaway drift if UI hangs
            float interp = (float)_interpolationTimer.Elapsed.TotalMilliseconds;
            displayTime += Math.Min(100, interp); 
        }
        
        _currentAudioTimeMs = displayTime;

        // Rocksmith 2014 Standard Colors
        uint[] rsColors = { 
            0xFF0000FF, // E (Red)
            0xFF00FFFF, // A (Yellow)
            0xFFFFFF00, // D (Blue)
            0xFF01A5FF, // G (Orange)
            0xFF00FF00, // B (Green)
            0xFFCC00FF  // e (Purple)
        };

        // 1. Draw Fretboard Shadow
        var pFarLeft = Project(-1.05f, Z_MAX, screenW, screenH);
        var pFarRight = Project(1.05f, Z_MAX, screenW, screenH);
        var pNearLeft = Project(-1.05f, 0, screenW, screenH);
        var pNearRight = Project(1.05f, 0, screenW, screenH);
        drawList.AddQuadFilled(pFarLeft.ScreenPos, pFarRight.ScreenPos, pNearRight.ScreenPos, pNearLeft.ScreenPos, 0xAA080808);

        // 2. Draw Vertical Frets (Metal dividers)
        for (int i = 0; i <= 24; i++)
        {
            float zFret = i * (Z_MAX / 8.0f); 
            if (zFret > Z_MAX) break;
            var fL = Project(-1.05f, zFret, screenW, screenH);
            var fR = Project(1.05f, zFret, screenW, screenH);
            drawList.AddLine(fL.ScreenPos, fR.ScreenPos, 0x66AAAAAA, 1f);
        }

        // 3. Draw Moving Beats
        foreach (var beat in Beats)
        {
            float tOffset = (beat - _currentAudioTimeMs) / 1000f;
            if (tOffset < 0 || tOffset > VISIBLE_SECONDS) continue;

            var bLeft = Project(-1.05f, tOffset * (Z_MAX / VISIBLE_SECONDS), screenW, screenH);
            var bRight = Project(1.05f, tOffset * (Z_MAX / VISIBLE_SECONDS), screenW, screenH);
            drawList.AddLine(bLeft.ScreenPos, bRight.ScreenPos, 0xAAFFFFFF, 2f * bLeft.Scale);
        }

        // 4. Draw Strings (Glow)
        for (int i = 0; i < 6; i++)
        {
            float stringT = -1.0f + (i * (2.0f / 5.0f)); 
            var sFar = Project(stringT, Z_MAX, screenW, screenH);
            var sNear = Project(stringT, 0, screenW, screenH);
            // Outer Glow
            drawList.AddLine(sFar.ScreenPos, sNear.ScreenPos, rsColors[i] & 0x44FFFFFF, 6f);
            // Core
            drawList.AddLine(sFar.ScreenPos, sNear.ScreenPos, rsColors[i] | 0x88000000, 2f);
        }

        // 5. Hitline Area
        drawList.AddLine(pNearLeft.ScreenPos, pNearRight.ScreenPos, 0xFFFFFFFF, 4f);

        // 6. Fretboard Numbers (Bottom Area)
        ImGui.PushFont(FontController.GetFontOfSize(24));
        int[] fretsToShow = { 0, 1, 3, 5, 7, 9, 12, 15, 17, 19, 21, 24 };
        foreach (int f in fretsToShow)
        {
            float xPos = -1.0f + (f / 12.0f); 
            var proj = Project(xPos, -0.1f, screenW, screenH);
            string fStr = f.ToString();
            var fSz = ImGui.CalcTextSize(fStr);
            drawList.AddText(proj.ScreenPos + new Vector2(-fSz.X/2, 15), 0xDDFFCC00, fStr);
        }
        ImGui.PopFont();

        // 7. Notes & Sustains (High-Visibility Gems)
        ImGui.PushFont(FontController.Font16_Icon16);
        foreach (var note in Notes)
        {
            float tStart = (note.Time - _currentAudioTimeMs) / 1000f;
            float tEnd = ((note.Time + note.Duration) - _currentAudioTimeMs) / 1000f;

            if (tEnd < 0 || tStart > VISIBLE_SECONDS) continue;

            float stringT = -1.0f + (note.String * (2.0f / 5.0f));
            uint noteColor = rsColors[note.String];

            // Sustain Ribbon
            if (note.Duration > 5)
            {
                var pS = Project(stringT, Math.Max(0, tStart) * (Z_MAX / VISIBLE_SECONDS), screenW, screenH);
                var pE = Project(stringT, Math.Min(VISIBLE_SECONDS, tEnd) * (Z_MAX / VISIBLE_SECONDS), screenW, screenH);
                float wS = (pNearRight.ScreenPos.X - pNearLeft.ScreenPos.X) * 0.08f * pS.Scale;
                float wE = (pNearRight.ScreenPos.X - pNearLeft.ScreenPos.X) * 0.08f * pE.Scale;

                drawList.AddQuadFilled(
                    new Vector2(pS.ScreenPos.X - wS/2, pS.ScreenPos.Y),
                    new Vector2(pS.ScreenPos.X + wS/2, pS.ScreenPos.Y),
                    new Vector2(pE.ScreenPos.X + wE/2, pE.ScreenPos.Y),
                    new Vector2(pE.ScreenPos.X - wE/2, pE.ScreenPos.Y),
                    noteColor & 0x44FFFFFF
                );
            }

            // Note Gem
            if (tStart >= 0)
            {
                var proj = Project(stringT, tStart * (Z_MAX / VISIBLE_SECONDS), screenW, screenH);
                float bw = (pNearRight.ScreenPos.X - pNearLeft.ScreenPos.X) * 0.14f * proj.Scale;
                float bh = 45f * proj.Scale;
                
                Vector2 tl = proj.ScreenPos - new Vector2(bw/2, bh/2);
                Vector2 br = proj.ScreenPos + new Vector2(bw/2, bh/2);
                
                // Black Border
                drawList.AddRectFilled(tl - new Vector2(2,2), br + new Vector2(2,2), 0xFF000000, 4f * proj.Scale);
                
                // Core Color
                drawList.AddRectFilled(tl, br, noteColor, 4f * proj.Scale);
                
                // Top Highlight
                drawList.AddRectFilled(tl, new Vector2(br.X, tl.Y + bh/3), 0x66FFFFFF, 4f * proj.Scale);

                // Bright Outline
                drawList.AddRect(tl, br, 0xFFFFFFFF, 4f * proj.Scale, ImDrawFlags.None, 1f);

                if (proj.Scale > 0.35f)
                {
                    string txt = note.Fret.ToString();
                    var sz = ImGui.CalcTextSize(txt);
                    drawList.AddText(proj.ScreenPos - (sz/2), 0xFF000000, txt);

                    // Technique Markers
                    string techIcon = "";
                    if (note.Techniques.HasFlag(NoteTechnique.HammerOn)) techIcon = "H";
                    else if (note.Techniques.HasFlag(NoteTechnique.PullOff)) techIcon = "P";
                    else if (note.Techniques.HasFlag(NoteTechnique.Slide)) techIcon = "S";
                    else if (note.BendValue > 0) techIcon = "B";

                    if (!string.IsNullOrEmpty(techIcon))
                    {
                        ImGui.PushFont(FontController.GetFontOfSize(14));
                        drawList.AddText(proj.ScreenPos + new Vector2(bw/2, -bh/2), 0xFFFFFFFF, techIcon);
                        ImGui.PopFont();
                    }
                }
            }
        }
        ImGui.PopFont();
    }

    public (Vector2 ScreenPos, float Scale) Project(float tX, float z, float screenW, float screenH)
    {
        // SLOPSMITH FORMULA
        float scale = Z_CAM / (z + Z_CAM);
        float y = screenH * (Y_HITLINE_NORM + (Y_HORIZON_NORM - Y_HITLINE_NORM) * (1.0f - scale));
        
        float hw = screenW * 0.52f * scale; // Width tapers with scale
        float x = (screenW / 2.0f) + (tX * hw);

        return (new Vector2(x, y), scale);
    }

    public void Dispose()
    {
        _waveOut?.Stop();

        _waveOut?.Dispose();
        _waveOut = null;

        _audioFileReader?.Dispose();
        _audioFileReader = null;

        if (!string.IsNullOrEmpty(CachedWavPath) && System.IO.File.Exists(CachedWavPath))
        {
            try { System.IO.File.Delete(CachedWavPath); } catch { }
        }
    }
}
