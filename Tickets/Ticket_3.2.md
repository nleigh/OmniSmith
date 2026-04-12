# Ticket 3.2: Base Highway Geometry

## Goal
Render the static elements of the guitar track (fretboard, strings, hitline).

## Instructions
1. **Draw the Fretboard (in `GuitarSong.Draw`)**:
   * Calculate the four corners of the fretboard panel using `Project(...)`.
   * For example, track width spans from `trackX = -300` to `trackX = +300`.
   * Corners:
     - Top Left: `Project(-300, 5000)` (Deep in Z axis)
     - Top Right: `Project(300, 5000)`
     - Bottom Left: `Project(-300, 0)` (At the hitline)
     - Bottom Right: `Project(300, 0)`
   * Use `drawList.AddQuadFilled` with a dark semi-transparent color.
2. **Draw String Lines**:
   * Loop `for i = 0 to 5`.
   * Calculate string position: `float strX = -250 + (i * 100);`
   * `Vector2 p1 = Project(strX, 5000).ScreenPos;`
   * `Vector2 p2 = Project(strX, 0).ScreenPos;`
   * `drawList.AddLine(p1, p2, stringColor[i], lineThickness);`
   * Use Rocksmith standard string colors (Red, Yellow, Blue, Orange, Green, Purple).
3. **Draw the Hitline**:
   * A vivid horizontal line spanning from `Project(-300, 0)` to `Project(300, 0)`.
4. **Draw Measure Lines / Beats**:
   * Loop through the `Beats` list. Calculate `zDepth = beat.Time - CurrentAudioTime`.
   * Only draw if `zDepth` is between 0 and 5000.
   * `drawList.AddLine(Project(-300, zDepth), Project(300, zDepth), white, 1);`
