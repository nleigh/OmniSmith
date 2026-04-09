# Ticket 7.1: Multi-Instrument Audio Mixer
**Goal**: Implement a professional audio mixer UI to independently control backing tracks and live instruments.

### Context
With the migration to `ManagedBass`, we gain the ability to route audio through separate channels. Players need a way to balance the CDLC backing track against their MIDI piano or Guitar input.

### Implementation Steps
1. **AudioManager Channels**:
   - Ensure `AudioManager` (ManagedBass) holds separate handles for `_backingTrackChannel` and `_instrumentChannel`.
2. **ImGui Mixer Window**:
   - Create `[NEW] Ui/Windows/AudioMixerWindow.cs`.
   - Implement vertical or horizontal sliders for:
     - **Master Volume**: Controls global BASS volume.
     - **Music Volume**: Controls the current `IPlayableSong` audio stream.
     - **SFX/Instrument Volume**: Controls the MIDI soundfont engine or guitar processing.
3. **Persistance**:
   - Bind these sliders to `CoreSettings`, ensuring the user's mix is saved and restored on the next launch.

### Definition of Done
A new "Audio Mixer" window is available in the UI. Moving the sliders correctly modifies the volume of the respective audio streams in real-time.
