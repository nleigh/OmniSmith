# Ticket 9.3: Audio-Reactive Environmental Shaders
**Goal**: Add visual flair that responds to the music.

### Context
Modern rhythm games often have backgrounds that pulse with the kick drum. We will use Veldrid/ImGui to implement this.

### Implementation Steps
1. **FFT Analysis**:
   - Use `ManagedBass` to perform a real-time FFT (Fast Fourier Transform) on the master audio channel.
2. **Uniform Distribution**:
   - Calculate a "Bass Intensity" value (sum of low-frequency bins) and a "Mid/Treble Intensity".
   - Pass these values as Uniforms into the Background Shader.
3. **Visual implementation**:
   - Update the background shader (or ImGui drawing code) to pulse the brightness/color of the highway or grid based on these intensities.

### Definition of Done
The highway background or grid lines pulse visibly in sync with the beat of the music.
