using ImGuiNET;
using OmniSmith.Core.Models;
using System.Collections.Generic;

namespace OmniSmith.Core.Interfaces;

public interface IPlayableSong : IDisposable
{
    string Title { get; }
    string Artist { get; }
    TimeSpan TotalDuration { get; }
    List<SongSection> Sections { get; }
    
    void Update(float currentAudioTimeMs);
    void Draw(ImDrawListPtr drawList);
}
