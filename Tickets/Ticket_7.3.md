# Ticket 7.3: Engine Hardening & Refactors
**Goal**: Finalize the architectural cleanup to ensure long-term maintainability.

### Context
`ScreenCanvas.cs` is currently a "God Class" with over 70,000 lines of code in the original Openthesia fork. This is a massive barrier to stability. We must break it down.

### Implementation Steps
1. **The Gigantic Class Purge**:
   - Extract the `RenderGrid`, `RenderMeasureLines`, and `DrawInputNotes` logic into a new `[NEW] src/OmniSmith/Core/Rendering/CommonRenderer.cs` utility.
   - Move all countdown/timer logic into a `[NEW] src/OmniSmith/Core/Services/PlaybackTimerService.cs`.
2. **Binary Packaging**:
   - Create a folder `[NEW] Resources/Binaries/`.
   - Place `vgmstream-cli.exe` and any FFmpeg DLLs here.
   - Update `RocksmithParser` to look for these binaries using `AppDomain.CurrentDomain.BaseDirectory` instead of assuming they are in the user's system PATH.
3. **Cross-Platform Audit**:
   - Audit `IOHandle.cs` and `Core/` for any direct calls to `user32.dll` or `kernel32.dll`. Wrap these in `#if WINDOWS` blocks or provide portable alternatives using Silk.NET.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
`ScreenCanvas.cs` is reduced by at least 50% in size. The application can run on a clean machine without manually installing vgmstream, as it is now self-contained.
