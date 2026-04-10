# OmniRhythm: Future Extreme & Stretch Ideas

This document formalizes high-level, "extreme" stretch goals for the OmniRhythm engine. These represent major shifts in project scope, ranging from AI model training to engine migrations.

---

## 🤖 1. The AI Ingestion Pipeline (YouTube-to-Song)
**Goal**: Create a fully automated "Zero-Click" pipeline to convert any music video into a playable MIDI/PSARC file.

### Concept
Leverage a local **RTX 5090** to run heavy multi-stage AI models:
1.  **Stage 1: Stem Separation**: Use [Meta's Demucs](https://github.com/facebookresearch/demucs) to split audio into Lead, Bass, Drums, and Piano.
2.  **Stage 2: Feature Extraction**: Use models like [Spotify's Basic Pitch](https://github.com/spotify/basic-pitch) or custom models (referencing [cdlc.ai](https://cdlc.ai/bass-model-v2)) to transcribe audio to MIDI.
3.  **Stage 3: Tab Inference**: Train a transformer model to infer "fingering" (strings/frets) based on note frequency and common guitar idioms.
4.  **Stage 4: Packaging**: Automatically generate the `.psarc` and `.xml` metadata to import directly into the OmniRhythm library.

---

## 🥁 2. The Percussion Domain (Drum Support)
**Goal**: Native support for electronic drum kits (MIDI) and plastic controller drums (YARG/Rock Band style).

### Implementation
-   **Domain C (Drums)**: A unique renderer for the "Lane-based" drum highway.
-   **Logic**: Map MIDI Note 36 (Kick), 38 (Snare), etc., to specific lanes.
-   **Legal Status**: Supporting MIDI input from hardware is 100% legal. Distributing copyrighted songs (CDLC) remains a gray area (user-sourced), but providing the *engine* is legally safe.
-   **Inspiration**: [YARG (Yet Another Rhythm Game)](https://github.com/YARC-Official/YARG) and [DTXmaniaNX](https://github.com/limyz/DTXmaniaNX).

---

## 🎮 3. Engine Evolution: The Unity Transition
**Goal**: Move from a C#/.NET ImGui application to a full-featured Unity game engine.

### Why Unity?
-   **Graphics**: Native support for **HDRP (High Definition Render Pipeline)** to enable high-end effects.
-   **Raytracing**: Use Unity's Raytracing API to render realistic reflections of the falling notes on reflective guitar strings and the polished wood of a virtual piano.
-   **VR/AR**: Ready-made SDKs for Meta Quest and Apple Vision Pro.
-   **Shader Graph**: Create complex audio-reactive visualizers without writing low-level HLSL.

### C# Potential
Since OmniRhythm is already authored in C# (.NET 6.0), the core parsing logic (`DryWetMIDI`, `Rocksmith2014.NET`) can be ported to Unity (using `.NET Standard 2.1`) with relatively low friction.

---

## 🚀 4. Additional Extreme Concepts

### 🕶️ Augmented Reality (AR) Piano Overlay
-   Use a Quest 3 or Vision Pro to project the "Falling Blocks" directly onto the keys of a physical piano.
-   **Hand Tracking**: Highlight the user's fingers in neon colors when they are correctly positioned over the next upcoming notes.

### ⚡ Real-time VST Integration
-   Instead of simple Soundfonts, host full **VST3** plugins (e.g., Kontakt, Serum) directly inside the engine for professional, studio-quality sound during practice.
