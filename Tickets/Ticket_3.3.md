# Ticket 3.3: Note & Sustain Rendering

## Goal
Draw the notes traveling down the highway toward the hitline.

## Instructions
1. **Inside `GuitarSong.Draw()`**:
   * Filter the `GuitarNotes` list: only process notes where `Note.Time + Note.Duration > CurrentAudioTime` and `Note.Time - CurrentAudioTime < 5000`.
2. **Render Sustain Trails**:
   * For notes with `Duration > 0`:
     * Start point: `Project(strX, note.Time - CurrentAudioTime)`
     * End point: `Project(strX, note.Time + note.Duration - CurrentAudioTime)`
     * `drawList.AddLine(p1, p2, stringColor, scaledThickness);`
3. **Render Note Heads**:
   * Calculate 3D projected position:
     `var proj = Project(stringX, note.Time - CurrentAudioTime);`
   * Only draw if `proj.Scale > 0` (on screen).
   * Note radius: `15f * proj.Scale`
   * Draw `AddRectFilled` or `AddCircleFilled` centered at `proj.ScreenPos`.
   * Overlay the `note.Fret` number inside the note using ImGui text if the scale allows it to be legible.
4. **Verify**:
   Run the game with a hardcoded mock note list (e.g., repeating notes every 500ms) to see them scroll correctly toward the camera!


### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.
