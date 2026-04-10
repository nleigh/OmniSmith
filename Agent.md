# OmniRhythm Agent Implementation Guide

Welcome context! You are an AI Agent tasked with building the OmniRhythm engine (merging OmniSmith and Slopsmith). 
You are working autonomously, step by step, picking up tasks from `Task Status.txt` and the `implementation_plan.md` (or Milestone blueprints). 

## Your Workflow:
1. **Context Initialization:** Read this `Agent.md` and understand the current architectural goal (Domain Separation via `IPlayableSong`).
2. **Select Task:** Open the [Project/agent_state.json](file:///Project/agent_state.json) schema file. Find the very first ticket where `"status": "incomplete"`. 
3. **Read the Spec:** Open the Markdown file specified in the `file_reference` attribute (e.g., `Tickets/Ticket_1.1.md`). Follow the ticket exactly.
4. **Execute:** Implement the feature as defined. Keep code clean and modular.
5. **Compile & Verify:** The codebase must build properly (`dotnet build`) and all tests in the test project must pass (`dotnet test`).
6. **Update Status:** Change `"incomplete"` to `"done"` in `Project/agent_state.json`.
7. **Commit:** Commit with a clear message (e.g., `git commit -m "Milestone 1, Ticket 1.1: Refactor IPlayableSong"`).

## Key Architecture Principles
* **No "Omni" God-Classes:** Keep domain logic isolated (Piano vs Guitar). Use the `IPlayableSong` interface for the main loop.
* **ManagedBass for Audio:** Use the **ManagedBass** library for all audio playback, time-stretching, and FFT processing.
* **Testing First:** Add unit tests to the `src/OmniSmith.Tests` project for core logic (math, parsing) as features are implemented.
* **ImGui Native Rendering:** Utilize `ImGui.NET` for the 3D highway rendering via math-based perspective projection in C#.
