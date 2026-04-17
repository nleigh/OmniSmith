# Ticket 10.2: Full Cross-Platform Portability
**Goal**: Ensure OmniRhythm runs on Windows, Linux, and macOS.

### Context
Openthesia has some hardcoded Windows logic. We must clean this up to ensure the engine is truly "Universal."

### Implementation Steps
1. **P/Invoke Cleanup**:
   - Locate any `[DllImport("user32.dll")]` or `[DllImport("kernel32.dll")]` calls.
   - Use `RuntimeInformation.IsOSPlatform` to provide fallback paths or use a cross-platform library like `Silk.NET` for window/monitor information.
2. **File System Paths**:
   - Ensure all paths use `Path.Combine` and respect `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` for Linux/Mac compliance.
3. **Build Validation**:
   - Test the build on a Linux environment (or use GitHub Actions/Local WSL).
   - Resolve any library linking issues for `NAudio` or `DryWetMIDI` on non-Windows targets.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The project compiles and runs with a single `dotnet run` command on both Windows and a Linux distribution (like Ubuntu/Arch), with audio and rendering working as expected.
