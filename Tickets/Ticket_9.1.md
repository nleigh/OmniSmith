# Ticket 9.1: 240FPS High-Refresh Mode
**Goal**: Optimize the core loop for fluid, high-refresh rate displays.

### Context
Rhythm games require low latency and high visual fluidity. We need to ensure the engine is fully frame-independent and can push high FPS.

### Implementation Steps
1. **DeltaTime Hardening**:
   - Audit `ScreenCanvas.cs`, `PianoSong.cs`, and `GuitarSong.cs`.
   - Ensure *every* movement (e.g., `n.Time += speed`) is correctly scaled by `ImGui.GetIO().DeltaTime`.
2. **VSync Control**:
   - Add a "VSync" toggle to `SettingsWindow.cs`.
   - When disabled, allow the Veldrid Swapchain to run as fast as possible.
3. **Telemetry**:
   - Ensure the FPS counter in the corner accurately reflects these high numbers and that there is no "jutting" or "stuttering" in note movement at high speeds.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The game can run at 240+ FPS on capable hardware. Note movement remains mathematically identical at 60 FPS and 240 FPS (no speed-ups or slow-downs).
