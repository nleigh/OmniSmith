# Ticket 3.5: Guitar Finger Tracking & Technique Labels
**Goal**: Overlay specific playing instructions and hand separation on the guitar highway.

### Context
Advanced CDLC files contain metadata for fingers (1-4) and techniques (Bend, Slide, Hammer-on). Applying these as visual overlays makes the highway much more informative.

### Implementation Steps
1. **Technique Rendering**:
   - In `GuitarSong.Draw()`, check the `GuitarNote.Techniques` flags.
   - Render a specific icon (e.g., an "H" for Hammer-on, a "P" for Pull-off) on top of the note block.
2. **Finger Numbers**:
   - If finger data is present in the `ChordTemplate` or `Note`, render a small number (1, 2, 3, or 4) in the center of the note block.
3. **Hand Coloring**:
   - For "Two-Handed Tapping" sections, use the `HandShape` data to color the notes differently (e.g., Pink for tapping hand) based on Openthesia's hand coloring logic.

### Definition of Done
The guitar highway displays technique icons and finger numbers. Notes change color during tapping sections to indicate the different hands, providing superior instructional feedback.
