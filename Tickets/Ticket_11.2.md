# Ticket 11.2: Performance Theatre (Replay System)
**Goal**: Record and playback instrument performances.

### Context
Users want to review their practice sessions. We will capture the raw event data to allow for "Perfect Quality" replays that can be viewed from any angle.

### Implementation Steps
1. **Event Capture**:
   - Create `[NEW] Core/Replays/ReplayManager.cs`.
   - Record every `NoteOn`, `NoteOff`, and `ControlChange` event from the user's instrument alongside a high-resolution timestamp.
2. **Persistence**:
   - Save these events to a `.omni_replay` file (JSON or binary).
3. **Playback Engine**:
   - When a replay is loaded, feed these recorded events back into the `IPlayableSong` renderer and the `SoundFontEngine`.

### Definition of Done
The user can "Record" a session. After finishing, they can "Load" the replay to watch their performance exactly as it happened, including the ability to change camera angles or enter VR mode during the replay.
