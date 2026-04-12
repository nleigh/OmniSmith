# Ticket 6.1: Plugin Architecture & Web Capabilities
**Goal**: Design an extensible web plugin architecture modeled after Slopsmith's Python extensions.

### Context
Slopsmith uses a plugin schema allowing easy scripting of scraping tools (e.g., Ultimate Guitar tab retrieval, CustomsForge integration). OmniRhythm should expose a standard interface for C# web scrapers to fetch and auto-import rhythm files.

### Implementation Steps
1. **The Plugin Interface**:
   - Create `[NEW] Core/Plugins/Web/IWebPlugin.cs`.
   - It should guarantee methods like `string GetPluginName()`, `Task<List<SearchResult>> SearchAsync(string query)`, and `Task<string> DownloadAsync(string id, string destinationFolder)`.
2. **Plugin Manager**:
   - Create `[NEW] Core/Plugins/Web/WebPluginManager.cs`.
   - Use reflection or explicit registrations to build a list of all active plugins.
3. **ImGui Download Window**:
   - Create a dedicated UI panel (`[NEW] Ui/Windows/BrowserWindow.cs`) that allows users to select a plugin from a dropdown, enter a search term, hit enter, and see a list of visual results.
   - Clicking a "Download" button on a result executes `DownloadAsync()` and saves the `.psarc` into the user's primary library folder. (The `LibraryScanner` built in Ticket 5.2 will automatically detect and index this new file).

### Definition of Done
The new interface exists and at least a mock plugin is registered. A user can search a term in the UI and download a dummy file seamlessly.
