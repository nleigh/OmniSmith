# Ticket 2.1: Domain Models

## Goal
Establish the core data structures that will hold Guitar (Rocksmith) arrangements. We need models that represent Guitar notes, chords, and the main Song container.

## Instructions
1. **Create `src\OmniSmith\Domains\Guitar\Models\NoteTechnique.cs`**:
   * Create a `[Flags]` enum `NoteTechnique` containing:
     `None = 0, Bend = 1, Slide = 2, UnpitchedSlide = 4, HammerOn = 8, PullOff = 16, Vibrato = 32, Harmonic = 64, PinchHarmonic = 128, Mute = 256, PalmMute = 512, Tremolo = 1024, Ignore = 2048`.
2. **Create `GuitarNote.cs`**:
   * Properties: `float Time`, `int String` (0-5), `int Fret`, `float Duration`, `NoteTechnique Techniques`, `float BendValue`, `int SlideTo`.
3. **Create `GuitarChord.cs`**:
   * Properties: `float Time`, `int ChordId`, `string Name`, `List<GuitarNote> ChordNotes`.
4. **Create `GuitarSong.cs: IPlayableSong` container** (`Domains/Guitar/GuitarSong.cs`):
   * This class implements `IPlayableSong`.
   * Add lists to hold `Notes`, `Chords`, `Beats` (time in Ms), `Anchors`.
   * Stubs:
     `public void Update(float audioTimeMs)`
     `public void Draw(ImDrawListPtr drawList)`
   * The `Draw` method will temporarily just draw "Loading Guitar Data..." via ImGui text in the center of the screen.

5. **Verify**:
   Run `dotnet build` to confirm all types resolve.
