# OmniRhythm Engine - Technical Specification

## 1. Executive Summary
The OmniRhythm Engine (OmniSmith) is a unified C# rhythm game platform. It merges the legacy MIDI visualization capabilities of **Openthesia** with the Rocksmith CDLC guitar highway mechanics of **Slopsmith**. By transitioning the monolithic architecture to a modular Domain Strategy pattern, OmniSmith can ingest and render an arbitrary number of music domains (e.g., Piano, Guitar, Drums) through a single, highly-optimized runtime frame.

## 2. Technology Stack
- **Language / Runtime:** C# 10.0 / .NET 6.0
- **Graphics Backend:** Veldrid (Polymorphic wrapper over DirectX/Vulkan/OpenGL)
- **UI Framework:** Dear ImGui (ImGui.NET)
- **Audio Engine:** NAudio (current). **ManagedBass** is the planned migration target for advanced playback, mixing, FFT, and time-stretch (not yet added as a dependency).
- **MIDI Parsing:** Melanchall DryWetMIDI
- **CDLC Parsing:** System.Xml.Linq (Handling Rocksmith2014 unencrypted `_lead.xml` definitions)

---

## 3. Core Architecture

### 3.1 The Domain Strategy Pattern
To support radically different file types (`.mid`, `.psarc`), the core engine loop is completely agnostic to musical logic. The relationship is governed by the `IPlayableSong` interface.

```csharp
public interface IPlayableSong : IDisposable
{
    string Title { get; }
    string Artist { get; }
    TimeSpan TotalDuration { get; }
    
    // Called once per frame in the main application loop
    void Update(float currentAudioTimeMs);
    void Draw(ImDrawListPtr drawList);
}
```

The application globally tracks the current state via `public static IPlayableSong CurrentSong`.

### 3.2 The Song Factory (Async Routing)
When a user inputs a file, the `SongFactory` determines the Domain parser.
- If `.mid`: Parses via DryWetMIDI on the main thread and instantiates `PianoSong`.
- If `.psarc`: Spawns a background task to unpack the archive, invokes `vgmstream-cli` to decode the audio payload to WAV, parses the XML, and returns a `GuitarSong`.

---

## 4. Domain Specifications

### 4.1 The Piano Domain (`PianoSong.cs`)
**Role:** Handles traditional piano visualization.
- **State Management:** Encapsulates the static `MidiFileData` payload.
- **Rendering:** Casts `ImGui.AddRectFilled()` to draw 2D cascading musical blocks mapping to an interactive 88-key piano ribbon rendered at the bottom of the screen.
- **Threading:** Uses DryWetMIDI's internal multi-threaded clock to execute `NoteCallback` structures, firing audio via generic SoundFonts or VST Plugins asynchronously.

### 4.2 The Guitar Domain (`GuitarSong.cs`)
**Role:** Handles 3D perspective rhythm highway for real guitar tablature.

#### 4.2.1 Data Models
The guitar domain utilizes specialized C# models parsed directly from Rocksmith CDLC schemas:
- `GuitarNote`: Translates String (0-5) and Fret coordinates.
- `GuitarChord`: Aggregates an array of `GuitarNote` instances.
- `NoteTechnique`: A `[Flags]` Enum representing articulations (Bends, Slides, PalmMutes, PullOffs).

#### 4.2.2 The 3D Render Pipeline
Because ImGui is inherently 2D, a mathematical projection utility transforms `(X: StringIndex, Z: NoteTime)` coordinates into scaled `(X, Y)` screen space.
- **Vanishing Point:** Defined dynamically based on `ImGui.GetWindowSize()`.
- **Highway Geometry:** The track is drawn using `AddQuadFilled` to create a perspective trapezoid.
- **Z-Sorting / Culling:** Only notes within `CurrentAudioTimeMs - 500 < Note.Time < CurrentAudioTimeMs + 5000` are evaluated for rendering.
- **Sustains:** Rendered as perspective-scaled polylines anchored between `StartTime` and `StartTime + Duration`.

---

## 5. File System & IO
- **Config & Settings:** Stored via `ProgramData.SettingsPath` and serialized as `Settings.json`. The application uses this centralized path abstraction for settings persistence.
- **Temporary Assets:** When Rocksmith `.psarc` files are unpacked, their WEM audio tracks are uncompressed into a temporary `.wav` cache stored in `AppDomain.CurrentDomain.BaseDirectory + "/cache/"`. This is managed and wiped inside `GuitarSong.Dispose()`.

## 6. Extensibility
By strict adherence to the `IPlayableSong` interface, new domains (such as a Drum layout parsing `.chart` files from Clone Hero) can be introduced natively. A developer only needs to write the parser and map the output to ImGui draw calls within their respective Domain folder without altering `Application.OnUpdate()`.
