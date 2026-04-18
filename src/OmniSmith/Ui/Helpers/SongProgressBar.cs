using ImGuiNET;
using OmniSmith.Core;
using OmniSmith.Core.Interfaces;
using OmniSmith.Core.Models;
using OmniSmith.Core.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OmniSmith.Ui.Helpers;

public static class SongProgressBar
{
    public static void Render(IPlayableSong? song, float currentMs, float totalMs)
    {
        if (song == null || totalMs <= 0) return;

        var drawList = ImGui.GetForegroundDrawList();
        float barHeight = 25f;
        Vector2 windowPos = ImGui.GetWindowPos();
        float barWidth = ImGui.GetWindowSize().X - 40;
        Vector2 pos = windowPos + new Vector2(20, 10); // Offset within window

        // Background
        Vector2 pMin = pos;
        Vector2 pMax = new Vector2(pos.X + barWidth, pos.Y + barHeight);
        drawList.AddRectFilled(pMin, pMax, 0x44000000, 5f);
        drawList.AddRect(pMin, pMax, 0x88FFFFFF, 5f);

        // Render Phrases/Sections from the shared interface
        var sections = song.Sections;
        if (sections != null && sections.Count > 0)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                float startX = (section.Time / totalMs) * barWidth;
                float endX = (i + 1 < sections.Count) ? (sections[i + 1].Time / totalMs) * barWidth : barWidth;
                
                uint sectionColor = GetSectionColor(section.Name);
                
                drawList.AddRectFilled(
                    new Vector2(pos.X + startX, pos.Y + 2),
                    new Vector2(pos.X + endX, pos.Y + barHeight - 2),
                    sectionColor & 0x66FFFFFF
                );

                if (barWidth > 800 && (endX - startX) > 60)
                {
                    ImGui.PushFont(FontController.GetFontOfSize(12));
                    drawList.AddText(new Vector2(pos.X + startX + 5, pos.Y + 5), 0xAAFFFFFF, section.Name);
                    ImGui.PopFont();
                }
            }
        }

        // Playhead
        float playheadX = Math.Clamp((currentMs / totalMs) * barWidth, 0, barWidth);
        drawList.AddLine(new Vector2(pos.X + playheadX, pos.Y - 5), new Vector2(pos.X + playheadX, pos.Y + barHeight + 5), 0xFF00FF00, 3f);

        // Interaction (Click and Drag to Seek) using ImGui idiomatic way
        ImGui.SetCursorScreenPos(pMin);
        ImGui.InvisibleButton("##SongProgressSeek", new Vector2(barWidth, barHeight));
        
        if (ImGui.IsItemActive())
        {
            Vector2 mousePos = ImGui.GetMousePos();
            float pct = Math.Clamp((mousePos.X - pMin.X) / barWidth, 0f, 1f);
            float seekTime = pct * totalMs;
            MidiPlayer.Timer = seekTime;
            
            // Only log sparingly if dragging
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                OmniSmith.Core.Logger.Info($"ProgressBar: Seeking to {seekTime:F0}ms ({pct*100:F1}%)");
            }
        }
    }

    private static uint GetSectionColor(string name)
    {
        // Authentic Rocksmith phrase colors (approximate)
        if (name.Contains("Verse")) return 0xFF00FF00;  // Green
        if (name.Contains("Chorus")) return 0xFF0000FF; // Red
        if (name.Contains("Bridge")) return 0xFFFFFF00; // Blue
        if (name.Contains("Solo")) return 0xFFFF00FF;   // Purple
        return 0xFFFFA500; // Orange for generic phrases
    }
}
