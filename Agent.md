# OmniRhythm Agent Implementation Guide

Welcome context! You are an AI Agent tasked with building the OmniRhythm engine (merging OmniSmith and Slopsmith). 
You are working autonomously, step by step, picking up tasks from `Task Status.txt` and the `implementation_plan.md` (or Milestone blueprints). 

## Your Workflow:
1. **Context Initialization:** Read this `Agent.md` and understand the current architectural goal (Domain Separation via `IPlayableSong`).
2. **Select Task:** Open the `agent_state.json` schema file. Find the very first ticket where `"status": "incomplete"`. 
3. **Read the Spec:** Open the Markdown file specified in the `file_reference` attribute (e.g., `Tickets/Ticket_1.1.md`). This file contains hyper-detailed, step-by-step instructions designed to fit within your context window. Follow it exactly.
4. **Execute:** Implement the feature as defined in the Ticket. Keep your code clean, modular, and following the Strategy Pattern.
5. **Compile & Verify:** The codebase must build properly (`dotnet build`) after every task. Never leave it in a broken state.
6. **Update Status:** Change `"incomplete"` to `"complete"` for that ticket in `agent_state.json`.
7. **Commit:** Commit the changes using git with a clear, small commit message (e.g., `git commit -m "Milestone 1, Ticket 1.1: Added IPlayableSong interface"`).
7. PR/Move on: Make pull requests if required, or simply move on to the next task in the file.

## Key Architecture Principles
* **No "Omni" God-Classes:** Keep Piano logic in `PianoSong` and Guitar logic in `GuitarSong`. The main loop should only know about `IPlayableSong`.
* **NAudio for Audio:** We rely on DryWetMIDI for MIDI events, but for `.psarc` audio, we use `NAudio` to load the decoded `.wav` files.
* **ImGui Native Rendering:** OmniRhythm relies on `ImGui.NET`. You will build the 3D highway using `drawList.AddQuadFilled`, `AddLine`, computing 3D perspective via pure math in C#. Do not use OpenGL directly.
