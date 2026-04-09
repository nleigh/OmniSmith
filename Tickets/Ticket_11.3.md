# Ticket 11.3: Unified "Omni" HUD & Aesthetics
**Goal**: Finalize the visual identity of the unified engine.

### Context
To make OmniRhythm feel like a single cohesive product, we need a shared "HUD" (Heads-Up Display) theme.

### Implementation Steps
1. **Shared HUD Layer**:
   - Create `[NEW] Ui/Components/SharedHud.cs`.
   - Implement common UI components: 
     - **Accuracy Streak**: A neon counter that grows/pulses as the player hits consecutive notes.
     - **Progress Bar**: A top-mounted bar showing song completion.
2. **Standardized Palettes**:
   - Define a global "OmniPalette" (Neon Cyan/Magenta/Green) used for all feedback (Perfect/Good/Miss).
3. **Application**:
   - Ensure these HUD elements are drawn over *both* the Piano domain and the Guitar domain.

### Definition of Done
Regardless of whether a user is playing a MIDI file or a PSARC file, the scoring feedback, counters, and overall UI aesthetic are perfectly consistent.
