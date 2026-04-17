# Ticket 5.2: Background Scanner & ImGui Integration
**Goal**: Wire the SQLite database to the file system and update ImGui data bindings.

### Context
The SQLite DB needs to stay perfectly in sync with the configured CDLC directory. We need a background thread that scans files, and the UI should pull from this DB efficiently.

### Implementation Steps
1. **Scanner Service**: 
   - Create `[NEW] src/OmniSmith/Core/Database/LibraryScanner.cs`.
   - Run a periodic background task (`Task.Run` / `Timer`) that reads all `.psarc` files in the configured library directory.
   - For newly discovered files (comparing `mtime` and `size` against the SQLite DB), extract the manifest metadata asynchronously and insert it into the `songs` table using `LibraryDatabase`.
2. **ImGui Browser Update**:
   - Locate `[MODIFY] Ui/Windows/MidiBrowserWindow.cs`.
   - Purge the old Logic that loops through `GameStateManager.Songs`.
   - Instead, compute the current visible view using `LibraryDatabase.QueryPage(searchString, currentPage, itemsPerPage)`.
   - Ensure you only draw elements that are returned from this paginated query so the ImGui loop stays at 60 FPS even with 10k songs.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The user can place hundreds of `.psarc` files into a folder. The background scanner seamlessly discovers them and adds them to DB. The ImGui MidiBrowserWindow displays them instantly across multiple pages without dropping frames.
