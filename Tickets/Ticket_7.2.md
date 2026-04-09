# Ticket 7.2: Visual Effects (VFX) & Shaders
**Goal**: Bring high-end visual polish to the rendering domains using particle systems and post-processing.

### Context
To compete with modern rhythm games, OmniRhythm needs dynamic feedback. We will implement simple VFX at the "Hit Line" and optional post-processing.

### Implementation Steps
1. **Particle System**:
   - Create `[NEW] Core/Rendering/ParticleSystem.cs`.
   - Implement a simple pool of "Spark" particles that can be spawned at a screen position with a color, velocity, and lifetime.
   - Inject this into `IPlayableSong.Update()` and `Draw()`.
2. **Domain Integration**:
   - **Piano**: Spawn sparks when a falling block hits the piano keys.
   - **Guitar**: Spawn sparks or "note explosions" when a note crosses the Hit Line.
3. **Advanced Shaders (Optional Refinement)**:
   - If using Veldrid, implement a `BloomPass.cs` or `ScanlineShader.cs` to give the 3D highway a neon, retro aesthetic.

### Definition of Done
The highway feels significantly more "alive." Successfully timed hits result in visual feedback (explosions/sparks) that fade out naturally.
