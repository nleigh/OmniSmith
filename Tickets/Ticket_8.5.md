# Ticket 8.5: Piano Session Mode (Backing Band)
**Goal**: Create a virtual band that follows the player's timing.

### Context
Standard MIDI playback is rigid. "Session Mode" allows the engine to follow the human player, adjusting the accompaniment speed in real-time.

### Implementation Steps
1. **Dynamic Tempo Detection**:
   - In `MidiPlayer`, monitor the time difference between the "Target Note" on the timeline and the "Actual Key Press" from the user.
2. **Servo Control**:
   - If the player is consistently late, slightly decrease the `MidiPlayer.Playback.Speed`.
   - If the player is early, slightly increase the speed.
   - Limit this adjustment to a ±15% window to prevent jarring tempo swings.
3. **Smooth Interpolation**:
   - Use a simple PID controller or smoothing function to ensure the speed changes are imperceptible.

### Definition of Done
The backing band (accompaniment tracks) follows the user's tempo. If the user slows down to master a difficult transition, the band slows with them automatically.
