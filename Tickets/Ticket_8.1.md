# Ticket 8.1: Riff Repeater (Loop & Level Up)
**Goal**: Implement an industry-standard practice system for looping and auto-speed incrementing.

### Context
One of Slopsmith's best features is the ability to loop sections. We want to bring this natively to the C# engine with "Level Up" capabilities.

### Implementation Steps
1. **Loop State**:
   - Add `IsLooping`, `LoopStartMs`, and `LoopEndMs` to `PlaybackTimerService`.
2. **Timeline Control**:
   - Update the `IPlayableSong.Update()` logic so that if `IsLooping` is true and `currentTime > LoopEndMs`, the timer seeks back to `LoopStartMs`.
3. **Adaptive Difficulty**:
   - Implement an "Adaptive Speed" flag. 
   - After each loop completes, check the current `AccuracyScoring` for that period.
   - If Accuracy > 95%, increment `MidiPlayer.Playback.Speed` by 5% (up to a max of 2.0).

### Definition of Done
The user can set loop points in the UI. When enabled, the song loops perfectly, and the playback speed automatically ramps up as the player hits notes accurately.
