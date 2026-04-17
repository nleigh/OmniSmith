# Ticket 2.3: Audio Extraction Pipeline

## Goal
Rocksmith audio is bundled inside `.psarc` files as Wwise Audio `.wem` files. We need to extract them and decode them to standard `.wav` or `.mp3` so `NAudio` can play them.

## Instructions
1. **Create `WemDecoder.cs` in `Domains/Guitar/Services`**:
   * Create method `public static string ConvertWemToWav(string wemFilePath)`.
   * We will rely on `vgmstream-cli`. 
   * Formulate the `ProcessStartInfo` to launch `vgmstream-cli -o output.wav input.wem`.
   * Ensure it returns the absolute path to `output.wav`.
2. **Audio Setup in `GuitarSong.cs`**:
   * Introduce NAudio dependencies inside `GuitarSong`. Look into `NAudio.Wave.AudioFileReader` and `NAudio.Wave.WaveOutEvent`.
   * Inside `GuitarSong.cs` constructor (or an async initialization method), load the `.wav` file.
   * `public void Play()` -> `waveOut.Play()`.
   * In `Update()`, sync your visual `audioTimeMs` to `audioFileReader.CurrentTime.TotalMilliseconds`.


### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.
