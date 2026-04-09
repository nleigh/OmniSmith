# Ticket 8.2: Chord Dictionary Overlay
**Goal**: Provide real-time finger position assistance for guitar players.

### Context
Unlike piano, guitar chords have complex finger patterns. A Chord Dictionary helps the user understand *how* to play a chord before it reaches the Hit Line.

### Implementation Steps
1. **Chord Metadata**:
   - Ensure the `GuitarSong` model has access to the "Chord Template" definitions in the Rocksmith XML.
2. **Detection Logic**:
   - Identify the "current upcoming chord" (the next chord to hit the Hit Line).
3. **UI Rendering**:
   - Create a small ImGui window named "Chord Guide".
   - Draw a 6-fret grid. 
   - Draw dots on the frets/strings for the current chord, labeled with finger numbers (1=Index, 4=Pinky) from the XML template.

### Definition of Done
When playing a PSARC file, a "Chord Guide" window appears. It updates dynamically to show the fingering for the next chord indicated on the highway.
