using ImGuiNET;
using System;

namespace OmniSmith.Core.Interfaces;

public interface IPlayableSong : IDisposable
{
    string Title { get; }
    string Artist { get; }
    TimeSpan TotalDuration { get; }
    
    void Update(float currentAudioTimeMs);
    void Draw(ImDrawListPtr drawList);
}
