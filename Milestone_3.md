# Milestone 3: The Guitar Renderer

## Overview
This milestone translates the Javascript WebGL canvas drawing logic from Slopsmith into C# utilizing `ImGui`. We will construct the 3D perspective highway to render strings, frets, hitlines, and incoming notes with sustains.

### 🎯 Key Objectives
- **3D Projection:** Build math functions to translate `(X, Z)` track coordinates into scaled `(X, Y)` screen coordinates.
- **Base Geometry:** Draw the trapezoidal fretboard, strings mapped to Rocksmith colors, and the Hit-Line.
- **Note Rendering:** Iterate over upcoming notes to draw gems, techniques, and extended perspective geometry for sustain tails.

### 📝 Tickets
Mathematical details, constants, and loops are fully outlined here:
- [Ticket 3.1: ImGui 3D Projection Utility](../Tickets/Ticket_3.1.md)
- [Ticket 3.2: Base Highway Geometry](../Tickets/Ticket_3.2.md)
- [Ticket 3.3: Note & Sustain Rendering](../Tickets/Ticket_3.3.md)
