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

### Definition of Done
A MIDI file starts with a simplified note density. As the player succeeds, the engine automatically adds the missing notes and chords until the full complexity is reached.
