# Ticket 3.4: MIDI-Synthesized Guitar Mode
**Goal**: Implement a synthesis-based fallback for the guitar audio track.

### Context
Standard time-stretching of WAV audio (as used in Rocksmith) becomes robotic below 50% speed. By using a MIDI soundfont to play the arrangement data, we can support clean practice at ultra-slow speeds (e.g., 1% speed for complex solos).

### Implementation Steps
1. **Audio Engine Bypass**:
   - Add a toggle in `GuitarSong` to mute the backing track.
2. **Soundfont Integration**:
   - Reuse the `SoundFontEngine` from the Piano domain.
   - Load a "Clean Electric Guitar" soundfont.
3. **MIDI Playback**:
   - Map the `GuitarNote` and `GuitarChord` data back to MIDI events (String/Fret -> Pitch).
   - Send these events to the SoundFontEngine synchronously with the highway rendering.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The user can mute the backing track and hear a synthesized guitar instead. The synthesized sound remains pitch-perfect even when the playback speed is set to 1%.
