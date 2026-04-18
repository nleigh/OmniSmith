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
    private const float VISIBLE_SECONDS = 8.0f; 
    private const float Z_CAM = 2.2f; 
    private const float Z_MAX = 10.0f;
    private const float Y_HITLINE_NORM = 0.82f; 
    private const float Y_HORIZON_NORM = 0.08f; 
    
    // UI State
    private float _fretWindowMin = 0f;
    private float _fretWindowMax = 12f;

    private System.Diagnostics.Stopwatch _interpolationTimer = new();
    private double _lastAudioTimeMs;

    public void Draw(ImDrawListPtr drawList)
    {
        Vector2 P = ImGui.GetWindowPos();
        float screenW = ImGui.GetWindowSize().X;
        float screenH = ImGui.GetWindowSize().Y;
        
        Vector2 V(float x, float y) => new Vector2(P.X + x, P.Y + y);

        // HIGH-RESOLUTION INTERPOLATION
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
            float interp = (float)_interpolationTimer.Elapsed.TotalMilliseconds;
            displayTime += Math.Min(100, interp); 
        }
        _currentAudioTimeMs = displayTime;

        // Dynamic Panning (Min/Max active frets)
        float minActiveFret = 24f;
        float maxActiveFret = 0f;
        foreach (var note in Notes)
        {
            float tOff = (note.Time - _currentAudioTimeMs) / 1000f;
            if (tOff > -2.0f && tOff < VISIBLE_SECONDS)
            {
                if (note.Fret > 0 && note.Fret < minActiveFret) minActiveFret = note.Fret;
                if (note.Fret > maxActiveFret) maxActiveFret = note.Fret;
            }
        }
        
        if (minActiveFret > maxActiveFret) minActiveFret = 1f;

        float targetMin = Math.Max(1f, minActiveFret - 1.5f);
        float targetMax = Math.Max(targetMin + 5f, maxActiveFret + 2f); // show at least 5 frets

        // Smoothly interpolate camera
        float dt = ImGui.GetIO().DeltaTime;
        _fretWindowMin += (targetMin - _fretWindowMin) * Math.Min(1f, 4f * dt);
        _fretWindowMax += (targetMax - _fretWindowMax) * Math.Min(1f, 4f * dt);
        // Rocksmith 2014 Standard Colors
        uint[] rsColors = { 
            0xFF0000FF, // E (Red)
            0xFF00FFFF, // A (Yellow)
            0xFFFFFF00, // D (Blue)
            0xFF01A5FF, // G (Orange)
            0xFF00FF00, // B (Green)
            0xFFCC00FF  // e (Purple)
        };
        uint[] rsDimColors = {
            0xFF000052,
            0xFF004252,
            0xFF522900,
            0xFF002952,
            0xFF295200,
            0xFF52003D
        };

        // 1. Draw Highway Background (Fades into distance)
        for (int i = 0; i < 40; i++)
        {
            float t0 = (i / 40f) * VISIBLE_SECONDS;
            float t1 = ((i + 1) / 40f) * VISIBLE_SECONDS;
            var p0 = Project(t0, screenW, screenH);
            var p1 = Project(t1, screenW, screenH);

            if (p0.Scale < 0 || p1.Scale < 0) continue;

            float hw0 = screenW * 0.26f * p0.Scale;
            float hw1 = screenW * 0.26f * p1.Scale;

            byte fade = (byte)(18 + 10 * p0.Scale);
            uint bgColor = 0xFF000000 | ((uint)(fade + 14) << 16) | ((uint)fade << 8) | fade;

            drawList.AddQuadFilled(
                V(screenW/2 - hw0, p0.ScreenY),
                V(screenW/2 + hw0, p0.ScreenY),
                V(screenW/2 + hw1, p1.ScreenY),
                V(screenW/2 - hw1, p1.ScreenY),
                bgColor
            );
        }

        // 2. Draw Fret Perspective Lines
        for (int fret = (int)Math.Max(1, Math.Floor(_fretWindowMin)); fret <= (int)Math.Ceiling(_fretWindowMax); fret++)
        {
            Vector2[] points = new Vector2[41];
            for (int i = 0; i <= 40; i++)
            {
                float t = (i / 40f) * VISIBLE_SECONDS;
                var p = Project(t, screenW, screenH);
                float x = FretX(fret, p.Scale, screenW);
                points[i] = V(x, p.ScreenY);
            }
            // Add polyline
            for (int i=0; i<40; i++) 
            {
                drawList.AddLine(points[i], points[i+1], 0xFF452D2D, 1f);
            }
        }

        // 3. Draw Moving Beats
        foreach (var beat in Beats)
        {
            float tOffset = (beat - _currentAudioTimeMs) / 1000f;
            if (tOffset < 0 || tOffset > VISIBLE_SECONDS) continue;
            var p = Project(tOffset, screenW, screenH);
            if (p.Scale < 0.06f) continue;
            
            float hw = screenW * 0.26f * p.Scale;
            drawList.AddLine(V(screenW/2 - hw, p.ScreenY), V(screenW/2 + hw, p.ScreenY), 0xFF503434, 2f);
        }

        // 4. Draw Horizontal Strings (Bottom)
        float hitY = screenH * Y_HITLINE_NORM;
        float strTop = screenH * 0.86f;
        float strBot = screenH * 0.94f;

        // Fret dividers across strings extending down from the hitline
        for (int fret = (int)Math.Max(1, Math.Floor(_fretWindowMin)); fret <= (int)Math.Ceiling(_fretWindowMax); fret++)
        {
            float x = FretX(fret, 1.0f, screenW);
            drawList.AddLine(V(x, hitY), V(x, screenH), 0xFF444444, 2f);
        }

        // Draw the 6 horizontal colored strings
        for (int i = 0; i < 6; i++)
        {
            float y = strTop + (i / 5.0f) * (strBot - strTop);
            drawList.AddLine(V(0, y), V(screenW, y), rsColors[i], 3f);
        }

        // 5. Draw Hitline / Now Line (Glow)
        float finalHw = screenW * 0.26f;
        drawList.AddLine(V(screenW/2 - finalHw, hitY), V(screenW/2 + finalHw, hitY), 0xFFF0E0DC, 3f);

        // 6. Draw Notes & Sustains
        ImGui.PushFont(FontController.Font16_Icon16);
        foreach (var note in Notes)
        {
            float tStart = (note.Time - _currentAudioTimeMs) / 1000f;
            float tEnd = ((note.Time + note.Duration) - _currentAudioTimeMs) / 1000f;

            uint noteColor = rsColors[note.String];
            uint dimColor = rsDimColors[note.String];

            // Sustain Ribbon
            if (note.Duration > 5 && tEnd >= 0 && tStart <= VISIBLE_SECONDS)
            {
                float t0 = Math.Max(tStart, 0);
                float t1 = Math.Min(tEnd, VISIBLE_SECONDS);
                if (t0 < t1)
                {
                    var p0 = Project(t0, screenW, screenH);
                    var p1 = Project(t1, screenW, screenH);
                    float x0 = FretX(note.Fret, p0.Scale, screenW);
                    float x1 = FretX(note.Fret, p1.Scale, screenW);
                    float sw0 = Math.Max(2f, 6f * p0.Scale);
                    float sw1 = Math.Max(2f, 6f * p1.Scale);

                    drawList.AddQuadFilled(
                        V(x0 - sw0, p0.ScreenY),
                        V(x0 + sw0, p0.ScreenY),
                        V(x1 + sw1, p1.ScreenY),
                        V(x1 - sw1, p1.ScreenY),
                        noteColor & 0x77FFFFFF
                    );
                }
            }

            // Note Gem
            if (tStart >= -0.05f && tStart <= VISIBLE_SECONDS)
            {
                var proj = Project(tStart, screenW, screenH);
                if (proj.Scale > 0)
                {
                    float x = FretX(note.Fret, proj.Scale, screenW);
                    float y = proj.ScreenY;
                    float sz = Math.Max(12f, 80f * proj.Scale * (screenH / 900f));
                    float half = sz / 2f;

                    if (note.Fret == 0) // Open string - draw horizontal bar
                    {
                        float hw = screenW * 0.26f * proj.Scale;
                        float barH = Math.Max(6f, sz * 0.45f);
                        drawList.AddRectFilled(V(screenW/2 - hw, y - barH/2), V(screenW/2 + hw, y + barH/2), noteColor, 2f);
                        
                        if (proj.Scale > 0.35f) {
                            var tsz = ImGui.CalcTextSize("0");
                            drawList.AddText(V(screenW/2 - tsz.X/2, y - tsz.Y/2), 0xFF000000, "0"); // Black text
                        }
                    }
                    else
                    {
                        // Standard Note
                        drawList.AddRectFilled(V(x - half, y - half), V(x + half, y + half), noteColor, sz / 5f);
                        drawList.AddRect(V(x - half, y - half), V(x + half, y + half), 0xFFFFFFFF, sz / 5f, ImDrawFlags.None, 1f);

                        if (proj.Scale > 0.35f)
                        {
                            string txt = note.Fret.ToString();
                            var tSz = ImGui.CalcTextSize(txt);
                            
                            // Text shadow
                            drawList.AddText(V(x - tSz.X/2 + 1, y - tSz.Y/2 + 1), 0x88000000, txt);
                            // Solid Black text
                            drawList.AddText(V(x - tSz.X/2, y - tSz.Y/2), 0xFF000000, txt);

                            // Technique Markers
                            string techIcon = "";
                            if (note.Techniques.HasFlag(NoteTechnique.HammerOn)) techIcon = "H";
                            else if (note.Techniques.HasFlag(NoteTechnique.PullOff)) techIcon = "P";
                            else if (note.Techniques.HasFlag(NoteTechnique.Slide)) techIcon = "S";
                            else if (note.BendValue > 0) techIcon = "b"+note.BendValue;

                            if (!string.IsNullOrEmpty(techIcon))
                            {
                                ImGui.PushFont(FontController.GetFontOfSize(14));
                                var iconSz = ImGui.CalcTextSize(techIcon);
                                drawList.AddText(V(x - iconSz.X/2, y - half - iconSz.Y - 2), 0xFFFFFFFF, techIcon);
                                ImGui.PopFont();
                            }
                        }
                    }
                }
            }
        }
        ImGui.PopFont();
        
        // 7. Fret Numbers (Bottom)
        ImGui.PushFont(FontController.GetFontOfSize(16));
        float fretY = screenH * 0.98f;
        for (int fret = (int)Math.Max(1, Math.Floor(_fretWindowMin)); fret <= (int)Math.Ceiling(_fretWindowMax); fret++)
        {
            float x = FretX(fret, 1.0f, screenW);
            string fStr = fret.ToString();
            var fSz = ImGui.CalcTextSize(fStr);
            drawList.AddText(V(x - fSz.X/2, fretY - fSz.Y/2), 0xFFAAAAAA, fStr);
        }
        ImGui.PopFont();
    }

    private float FretX(int fret, float scale, float w)
    {
        float hw = w * 0.52f * scale;
        float margin = hw * 0.06f;
        float usable = hw * 2f - 2f * margin;
        
        // Normalize against our dynamic sliding camera window!
        float length = Math.Max(1f, _fretWindowMax - _fretWindowMin);
        float t = (fret - _fretWindowMin) / length;
        
        return w / 2f - hw + margin + t * usable;
    }

    public (float ScreenY, float Scale) Project(float tOffset, float screenW, float screenH)
    {
        if (tOffset > VISIBLE_SECONDS || tOffset < -0.05f) return (-1f, -1f);
        if (tOffset < 0f) return (screenH * (Y_HITLINE_NORM + Math.Abs(tOffset) * 0.3f), 1.0f);

        float z = tOffset * (Z_MAX / VISIBLE_SECONDS);
        float denom = z + Z_CAM;
        if (denom < 0.01f) return (-1f, -1f);
        float scale = Z_CAM / denom;
        float y = Y_HITLINE_NORM + (Y_HORIZON_NORM - Y_HITLINE_NORM) * (1.0f - scale);
        
        return (y * screenH, scale);
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
