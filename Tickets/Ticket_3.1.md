# Ticket 3.1: ImGui 3D Projection Utility

## Goal
The guitar highway is 3D (perspective). ImGui is 2D. We need mathematical projection to take `(X=string, Y=time)` and project to `(X=screenX, Y=screenY, Scale=zScale)`.

## Instructions
1. **Create Projection logic in `GuitarSong.cs`**:
   * Add constants/variables:
     ```csharp
     float VanishingPointX = ImGui.GetIO().DisplaySize.X / 2f;
     float HorizonY = ImGui.GetIO().DisplaySize.Y * 0.2f;
     float HitLineY = ImGui.GetIO().DisplaySize.Y * 0.9f;
     float FieldOfView = 800f; // Zoom factor
     ```
   * Create `(Vector2 ScreenPos, float Scale) Project(float trackX, float zDepth)`
   * **[LOCAL AGENT HELP:]** Refer to this Javascript implementation from `slopsmith/highway.js` when building the math:
     ```javascript
     function projectToScreen(x, z, canvas) {
         let scale = FOCAL_LENGTH / (FOCAL_LENGTH + z);
         let px = (canvas.width / 2) + x * scale;
         let py = HORIZON_Y + (canvas.height - HORIZON_Y) * (1 - scale);
         return { x: px, y: py, scale: scale };
     }
     ```
     Translate this to C#, ensuring `zDepth` maps linearly to `py` from Horizon to Hitline.
     * `zDepth` is `(NoteTime - CurrentAudioTimeMs) * ScrollSpeed`.
     * If `zDepth < 0` (note is past the hitline), still render it if it has sustain, but clamp or handle math so it flies toward the camera.

2. **Verify**:
   * Add a debug ImGui text overlay printing out `Project(0, 500)` just to verify the math evaluates correctly without NaN or Infinity.


### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.
