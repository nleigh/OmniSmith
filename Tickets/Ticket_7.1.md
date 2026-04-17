# Ticket 7.1: Multi-Instrument Audio Mixer
**Goal**: Implement a professional audio mixer UI to independently control backing tracks and live instruments.

### Context
With the migration to `NAudio`, we gain the ability to route audio through separate channels. Players need a way to balance the CDLC backing track against their MIDI piano or Guitar input.

### Implementation Steps
1. **AudioManager Channels**:
   - Ensure `AudioManager` (NAudio) holds separate handles for `_backingTrackChannel` and `_instrumentChannel`.
2. **ImGui Mixer Window**:
   - Create `[NEW] src/OmniSmith/Ui/Windows/AudioMixerWindow.cs`.
   - Implement vertical or horizontal sliders for:
     - **Master Volume**: Controls global BASS volume.
     - **Music Volume**: Controls the current `IPlayableSong` audio stream.
     - **SFX/Instrument Volume**: Controls the MIDI soundfont engine or guitar processing.
3. **Persistence**:
   - Bind these sliders to `CoreSettings`, ensuring the user's mix is saved and restored on the next launch.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
A new "Audio Mixer" window is available in the UI. Moving the sliders correctly modifies the volume of the respective audio streams in real-time.

### Context
`ScreenCanvas.cs` is currently a "God Class" with over 70,000 lines of code in the original Openthesia fork. This is a massive barrier to stability. We must break it down.
