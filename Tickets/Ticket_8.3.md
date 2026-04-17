# Ticket 8.3: Piano Dynamic Difficulty (DDC)
**Goal**: Make the Piano domain as accessible as Rocksmith by gradually introducing song complexity.

### Context
Unlike Rocksmith, Openthesia currently shows every note in a MIDI file. This is overwhelming for beginners. We will implement "Level" based filtering.

### Implementation Steps
1. **Simplified View Logic**:
   - Implement an `ArrangementLevel` integer (0-10).
   - Create a filter in `PianoSong.Update()`:
     - Level 0: Only play the "loudest" notes (melody).
     - Level 5: Add roots of chords.
     - Level 10: Full complexity.
2. **Adaptive Logic**:
   - Link this to `AccuracyScoring`. 
   - After a successful `RiffRepeater` loop with > 90% accuracy, increment the `ArrangementLevel`.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
A MIDI file starts with a simplified note density. As the player succeeds, the engine automatically adds the missing notes and chords until the full complexity is reached.
