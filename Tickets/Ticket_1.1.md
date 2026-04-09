# Ticket 1.1: Interface Extraction

## Goal
To allow OmniSmith to play things other than MIDI (like Rocksmith audio), we must break its direct dependency on `MidiFileData` and `MidiPlayer` inside the rendering loop. We'll introduce the `IPlayableSong` interface.

## Instructions for Agent
1. **Create `src\OmniSmith\Core\Interfaces\IPlayableSong.cs`**:
   ```csharp
   using ImGuiNET;
   using System;
   namespace OmniSmith.Core.Interfaces;

   public interface IPlayableSong : IDisposable
   {
       string Title { get; }
       string Artist { get; }
       TimeSpan TotalDuration { get; }
       void Update(float audioTimeMs); 
       void Draw(ImDrawListPtr drawList); 
   }
   ```
2. **Create `src\OmniSmith\Domains\Piano\PianoSong.cs`**:
   * This class must implement `IPlayableSong`.
   * In its constructor `public PianoSong(string filePath)`, call `OmniSmith.Core.Midi.MidiFileHandler.LoadMidiFile(filePath)` and populate `Title` and `TotalDuration` from `MidiFileData` (use `TimeSpan.FromTicks(duration.TotalMicroseconds * 10)` to be .NET 6 compatible).
   * Inside `Update(float audioTimeMs)`, leave it blank for now as MIDI events trigger on their own thread.
   * Inside `Draw(ImDrawListPtr drawList)`, you must wrap the existing drawing logic. Since `MidiPlaybackWindow` used to render it, copy the ImGui code that renders "Screen" (`ScreenCanvas.RenderCanvas(false)`) and "Keyboard" (`PianoRenderer.RenderKeyboard()`) into this `.Draw()` method to isolate it.

3. **Verify**:
   Run `dotnet build`. There should be no breaking errors since we are just adding new files. Proceed to Ticket 1.2 to integrate it.
