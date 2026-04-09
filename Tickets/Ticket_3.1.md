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
     * `zDepth` is `(NoteTime - CurrentAudioTimeMs) * ScrollSpeed`.
     * If `zDepth < 0` (note is past the hitline), still render it if it has sustain, but clamp or handle math so it flies toward the camera.
     * `float scale = FieldOfView / (FieldOfView + zDepth);`
     * `float screenX = VanishingPointX + (trackX * scale);`
     * `float screenY = HorizonY + ((HitLineY - HorizonY) * (1.0f - scale));` // Simplified perspective Y

2. **Verify**:
   * Add a debug ImGui text overlay printing out `Project(0, 500)` just to verify the math evaluates correctly without NaN or Infinity.
