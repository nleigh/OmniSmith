# Ticket 11.2: Unified "Omni" HUD & Aesthetics
**Goal**: Finalize the visual identity of the unified engine.

### Context
To make OmniRhythm feel like a single cohesive product, we need a shared "HUD" (Heads-Up Display) theme.

### Implementation Steps
1. **Shared HUD Layer**:
   - Create `[NEW] src/OmniSmith/Ui/Components/SharedHud.cs`.
   - Implement common UI components: 
     - **Accuracy Streak**: A neon counter that grows/pulses as the player hits consecutive notes.
     - **Progress Bar**: A top-mounted bar showing song completion.
2. **Standardized Palettes**:
   - Define a global "OmniPalette" (Neon Cyan/Magenta/Green) used for all feedback (Perfect/Good/Miss).
3. **Application**:
   - Ensure these HUD elements are drawn over *both* the Piano domain and the Guitar domain.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
Regardless of whether a user is playing a MIDI file or a PSARC file, the scoring feedback, counters, and overall UI aesthetic are perfectly consistent.
