# Ticket 9.2: Multi-Instrument Jam Mode
**Goal**: Support dual-instrument playback in a single session.

### Context
To allow for local "jams," the engine needs to support rendering two domains at once.

### Implementation Steps
1. **Multi-Song State**:
   - Update `Program.cs` or `GameStateManager` to hold `IPlayableSong[] _activeSongs`.
2. **Layout Engine**:
   - In `ScreenCanvas.RenderCanvas`, if multiple songs are active, divide the screen vertically.
   - Give each song a sub-view (using `ImGui.BeginChild` or `drawList` offsets).
3. **Synchronization**:
   - Ensure both songs are pinned to the same `PlaybackTimerService` instance so they never drift apart.

### Definition of Done
The user can select "Jam Mode" and load both a `.mid` and a `.psarc`. Both the 2D piano waterfall and the 3D guitar highway appear side-by-side and play perfectly in sync.
