# Ticket 4.2: Asynchronous Song Factory

## Goal
Avoid blocking the UI thread while extracting `.wem` files and parsing deep XML trees from Rocksmith bundles.

## Instructions
1. **Create `SongFactory.cs` in `Openthesia/Openthesia/Core`**:
   * `public static async Task<IPlayableSong> LoadSongAsync(string filePath)`
   * Check the extension.
   * If `.mid`, return `new PianoSong(filePath)`. (PianoSong can remain sync for now as DryWetMIDI usually parses quickly).
   * If `.psarc`, return `await Task.Run(() => RocksmithParser.ParsePsarc(filePath))`.
2. **Update Main Loading Flow in `Application.cs`**:
   * Add a `public static bool IsLoading { get; set; }` variable.
   * When loading a queued song:
     ```csharp
     Application.IsLoading = true;
     // Fire-and-forget or async void if necessary, but safely assign CurrentSong
     _ = Task.Run(async () => {
         var song = await SongFactory.LoadSongAsync(nextFile);
         Application.CurrentSong?.Dispose();
         Application.CurrentSong = song;
         Application.IsLoading = false;
         // Start timers...
     });
     ```
3. **Show Loading Overlay in `MidiPlaybackWindow.cs`**:
   * In `OnImGui()`, if `Application.IsLoading`, draw a center-screen ImGui Spinner or simple text that says "Extracting CDLC Audio...".
4. **Verify**:
   Load a heavy PSARC file and verify the application frame rate remains smooth while extracting!
