# OmniRhythm Agent Implementation Guide

Welcome context! You are an AI Agent tasked with building the OmniRhythm engine (merging OmniSmith and Slopsmith). 
You are working autonomously, step by step, picking up tasks from `Project/agent_state.json` and the Milestone blueprints in `Tickets/Milestones/`. 

## Your Workflow:
1. **Context Initialization:** Read this `Agent.md` and understand the current architectural goal (Domain Separation via `IPlayableSong`).
2. **Select Task:** Open the [Project/agent_state.json](Project/agent_state.json) schema file. Find the very first ticket where `"status": "incomplete"`. 
3. **Read the Spec:** Open the Markdown file specified in the `file_reference` attribute (e.g., `Tickets/Ticket_1.1.md`). Follow the ticket exactly.
4. **Execute:** Implement the feature as defined. Keep code clean and modular.
5. **Compile & Verify:** The codebase must build properly (`dotnet build`) and all tests in the test project must pass (`dotnet test`).
6. **Update Status:** Change `"incomplete"` to `"done"` in `Project/agent_state.json`.
7. **Commit:** Commit with a clear message (e.g., `git commit -m "Milestone 1, Ticket 1.1: Refactor IPlayableSong"`).

## Key Architecture Principles
* **No "Omni" God-Classes:** Keep domain logic isolated (Piano vs Guitar). Use the `IPlayableSong` interface for the main loop.
* **NAudio for Desktop Playback:** We utilize **NAudio** (`NAudio.Wave.AudioFileReader` and `WaveOutEvent`) for local playback on `GuitarSong` domains. NEVER use `ManagedBass` or obsolete audio tools.
* **ImGui Native Rendering:** Utilize `ImGui.NET` for the 3D highway rendering via math-based perspective projection in C#.
* **Zero Stub Policy:** NEVER output `throw new NotImplementedException();` or leave `TODO` stubs. Implement functions from start to finish securely, including valid error throwing logic when interacting with external binaries.

## 🚨 Common Mistakes to Avoid (From Code Reviews)
- **Audio Thread Locking:** Never assign a new `GuitarSong` backing track without first executing `.Dispose()` on the overarching `Application.CurrentSong`. Operating system hardware will fatally lock your application if 2 audio streams sync concurrently. Furthermore, verify you explicitly route `_waveOut?.Stop();` down through class `Dispose()` pipelines to prevent ghost audio.
- **Async Spawning Completion Races:** If utilizing `System.Diagnostics.Process` closures, NEVER use `TaskCompletionSource` bound arbitrarily to `.Exited` event handles. You must enclose external runs using `using var process` combined explicitly sequentially traversing `await process.WaitForExitAsync()`. Catch `Win32Exceptions` dynamically if tools don't map to ENV PATH bounds. ALWAYS write code to capture and test `.ExitCode == 0` validation checking to prevent processing corrupted mock files safely.
- **Screen Canvas Bypassing:** When configuring custom structural layout branching inside `Ui/ScreenCanvas.cs`, NEVER end an `if` tree block with an automatic early `return;`. This kills execution logic prematurely causing FPS counters, play-pause controls, global variables, and overlapping shared UI parameters to visibly disappear. Route exclusively around custom block requirements via `if-else` negative inversion bindings securely returning natural structural behavior bounds.

## Test-Driven Execution (TDD)
- **Mandatory Unit Testing:** Every task requiring `File IO`, database mutation checks, or executing external CLI commands demands the construction of internal XUnit validations within `src/OmniSmith.Tests/`.
- Ensure you simulate/mock path failures utilizing testing bounds explicitly verifying application safety constraints won't throw unhandled bounds issues securely prior to completing your current payload branch.
