# Ticket 8.4: Piano Tone Automation
**Goal**: Implement automatic instrument switching for MIDI playback.

### Context
Rocksmith automatically switches "Tones" (Clean/Dirty) during a song. We can replicate this for piano by switching Soundfonts or VST presets based on song sections.

### Implementation Steps
1. **Instrument Mapping**:
   - Add a `TargetInstrument` field to the `Section` model in `PianoSong`.
2. **Switching Logic**:
   - During `Update()`, if a section change is detected, trigger a `LoadSoundFont` call to the `SoundFontEngine`.
3. **Preset Configuration**:
   - Allow the user to define "Default Verse Instrument" and "Default Chorus Instrument" in the MIDI settings if specific data isn't in the file.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The piano sound changes automatically as the song transitions between different sections (e.g., Intro, Verse, Chorus), providing an immersive "Active Mix" experience.
