# Ticket 1.2: State Management & Main Loop

## Goal
Replace the global static access to MIDI playback in the primary Application loop, and update the UI window to render whatever is currently in `CurrentSong`.

## Instructions for Agent
1. **Update `Openthesia\Openthesia\Core\Application.cs`**:
   * Add a property: `public static Openthesia.Core.Interfaces.IPlayableSong CurrentSong { get; set; }`
   * In `OnUpdate()`, when `MidiPlayer.ShouldAdvanceQueue` triggers a `nextFile` load:
     Replace `MidiFileHandler.LoadMidiFile(nextFile);` with:
     ```csharp
     Application.CurrentSong?.Dispose();
     Application.CurrentSong = new Openthesia.Domains.Piano.PianoSong(nextFile);
     ```

2. **Update `Openthesia\Openthesia\Ui\Windows\MidiBrowserWindow.cs`**:
   * Around line 62, replacing `MidiFileHandler.LoadMidiFile(file);` with:
     ```csharp
     Application.CurrentSong?.Dispose();
     Application.CurrentSong = new Openthesia.Domains.Piano.PianoSong(file);
     ```

3. **Update `Openthesia\Openthesia\Ui\Windows\MidiPlaybackWindow.cs`**:
   * Completely clear out the `OnImGui` function and replace it with:
    ```csharp
    protected override void OnImGui()
    {
        if (Application.CurrentSong != null)
        {
            Application.CurrentSong.Update(0f);
            Application.CurrentSong.Draw(ImGui.GetWindowDrawList());
        }
    }
    ```
   * *Note: Any lingering scoring logic (like `RenderScoringHeatmap`) that was originally in `MidiPlaybackWindow.cs` should be relocated inside `PianoSong.cs` so it only shows up during piano sessions.*

4. **Verify**:
   Run `dotnet run`. The application should compile and MIDI files should still open, render the falling blocks, keyboard, and play sounds identically to how they did before.
