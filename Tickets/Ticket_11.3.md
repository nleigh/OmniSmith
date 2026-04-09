# Ticket 11.3: Cross-Instrument Playlists
**Goal**: Unified library management for all media types.

### Context
Players often want to switch between guitar and piano without diving back into the OS file explorer. A unified playlist system solves this.

### Implementation Steps
1. **Playlist Metadata**:
   - Update `LibraryDatabase` to support a `Playlists` table mapping a playlist name to a list of file paths.
2. **Media Agnostic Loading**:
   - In the `PlaylistController`, iterate through the list. 
   - When the next item is triggered, call the `Async Domain Routing` logic from Milestone 4.
3. **UI Integration**:
   - Add a "Add to Playlist" button in the `MidiBrowserWindow`.
   - Create a `[NEW] Ui/Windows/PlaylistWindow.cs` to manage and launch these sets.

### Definition of Done
The user can create a playlist containing both `.mid` and `.psarc` files. The engine transitions perfectly between instruments, loading the correct domain and renderer for each song in the sequence.
