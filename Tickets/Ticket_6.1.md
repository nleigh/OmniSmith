# Ticket 6.1: Plugin Architecture & Web Capabilities
**Goal**: Design an extensible web plugin architecture modeled after Slopsmith's Python extensions.

### Context
Slopsmith uses a plugin schema allowing easy scripting of scraping tools. OmniRhythm should expose a standard interface for C# web scrapers to fetch and auto-import rhythm files.

### Implementation Steps
1. **The Plugin Interface**:
   - Create `[NEW] src/OmniSmith/Core/Plugins/Web/IWebPlugin.cs`.
   - It should guarantee methods like `string GetPluginName()`, `Task<List<SearchResult>> SearchAsync(string query)`, and `Task<string> DownloadAsync(string id, string destinationFolder)`.
2. **Plugin Manager**:
   - Create `[NEW] src/OmniSmith/Core/Plugins/Web/WebPluginManager.cs`.
   - Implement `TryGetAvailablePlugins()` iterating explicit registrations securely. Add standard `try-catch` blocks surrounding any explicit web parsing requests to ensure it never crashes the main frame loop.
3. **ImGui Download Window**:
   - Create a dedicated UI panel (`[NEW] src/OmniSmith/Ui/Windows/BrowserWindow.cs`) utilizing standard `if-else` bypassed bindings (no early renders!) that allows users to select a plugin from a dropdown, enter a search term, hit enter, and see a list of visual results.
   - Implement an explicit asynchronous `Loading Spinner` whenever `SearchAsync` is called so the user isn't stuck natively.

### Mandatory TDD Generation 🧪
- You **must** create `[NEW] src/OmniSmith.Tests/Core/Plugins/WebPluginTests.cs` implementing explicit `xUnit` mock frameworks covering fake search result strings mimicking `IWebPlugin`. Verify bounds so empty result collections don't throw unchecked null-reference exceptions on the screen render loops.

### Definition of Done
The new interfaces exist, `dotnet test` evaluates securely over the mock constraints, and an ImGui search bar successfully displays dummy strings directly locally without thread-locking structural freezes.
