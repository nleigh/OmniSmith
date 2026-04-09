# Ticket 11.1: Guitar "Waterfall" Sight-Reading Mode
**Goal**: Implement a 2D sight-reading alternative for the Guitar domain.

### Context
A 2D vertical waterfall is often easier for sight-reading dense sections than the 3D perspective highway.

### Implementation Steps
1. **Vertical Projection**:
   - Add a `RendererMode` (Perspective vs Waterfall) to the `GuitarSong` class.
   - If Waterfall is active, bypass the 3D projection math.
   - Map each of the 6 strings to a vertical column on a 2D plane (similar to the Piano domain).
2. **Note Geometry**:
   - Render notes as rectangular blocks with width equal to the string column.
3. **Toggle UI**:
   - Add a button in the in-game HUD to switch modes instantly.

### Definition of Done
The user can toggle between the 3D highway and a 2D vertical waterfall for any CDLC song. Both render modes remain in sync with the audio.
