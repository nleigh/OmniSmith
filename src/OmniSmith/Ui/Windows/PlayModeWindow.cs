using ImGuiNET;
using OmniSmith.Core;
using OmniSmith.Core.Interfaces;
using OmniSmith.Core.Models;
using OmniSmith.Core.Midi;
using OmniSmith.Settings;
using System;
using System.Numerics;

namespace OmniSmith.Ui.Windows;

public class PlayModeWindow : ImGuiWindow
{
    public PlayModeWindow()
    {
        _id = Enums.Windows.PlayMode.ToString();
        _active = false;
    }

    protected override void OnImGui()
    {
        Vector2 canvasSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y * 75 / 100);
        if (ImGui.BeginChild("Screen", canvasSize))
        {
            ScreenCanvas.RenderCanvas(true);
            
            // Header: Back button
            ImGui.SetCursorPos(new Vector2(10, 10));
            ImGui.PushFont(FontController.Font16_Icon16);
            if (ImGui.Button($"{IconFonts.FontAwesome6.ArrowLeftLong} Back", new Vector2(100, 35)) || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                MidiPlayer.StopTimer();
                if (MidiPlayer.Playback != null) MidiPlayer.Playback.Stop();
                WindowsManager.SetWindow(Enums.Windows.MidiBrowser);
            }
            ImGui.PopFont();

            // Render the Phrase/Progress bar at the top of the highway
            float totalMs = (float)(Application.CurrentSong?.TotalDuration.TotalMilliseconds ?? 0);
            OmniSmith.Ui.Helpers.SongProgressBar.Render(Application.CurrentSong, MidiPlayer.Timer, totalMs);

            ImGui.EndChild();
        }

        Vector2 lineStart = new(0, ImGui.GetCursorPos().Y);
        Vector2 lineEnd = new(ImGui.GetContentRegionAvail().X, ImGui.GetCursorPos().Y);
        uint lineColor = ImGui.GetColorU32(new Vector4(0.529f, 0.784f, 0.325f, 1f));
        const float lineThickness = 2f;
        ImGui.GetForegroundDrawList().AddLine(lineStart, lineEnd, lineColor, lineThickness);

        if (ImGui.BeginChild("Keyboard", ImGui.GetContentRegionAvail()))
        {
            PianoRenderer.RenderKeyboard();
            ImGui.EndChild();
        }
    }
}
