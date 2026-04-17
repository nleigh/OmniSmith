# Ticket 10.1: VR Support (First Person Camera)
**Goal**: Allow players to view the highway in Virtual Reality.

### Context
Using the 3D perspective math already in place, we can easily support VR headsets by mapping their orientation to the camera view.

### Implementation Steps
1. **Camera Refactor**:
   - Ensure the `ProjectToScreen` logic in `GuitarSong` uses a generic `ViewMatrix` and `ProjectionMatrix` instead of hardcoded Vanishing Points.
2. **OpenXR/SteamVR Integration**:
   - Add a lightweight VR library (e.g., Silk.NET.OpenXR).
   - Retrieve Headset Pose and apply it to the `ViewMatrix`.
3. **Stereo Rendering**:
   - Render the highway twice (once for each eye) with a slight horizontal offset representing Inter-Pupillary Distance (IPD).



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
The user can toggle "VR Mode". Wearing a headset allowed them to see the 3D highway in immersive 3D, moving their head to look around the stage.
