# OmniRhythm Agent Implementation Guide

Welcome context! You are an AI Agent tasked with building the OmniRhythm engine (merging Openthesia and Slopsmith). 
You are working autonomously, step by step, picking up tasks from `Task Status.txt` and the `implementation_plan.md` (or Milestone blueprints). 

## Your Workflow:
1. **Context Initialization:** Read this `Agent.md`, `Task Status.txt`, and understand the current architectural goal (Domain Separation via `IPlayableSong`).
2. **Select Task:** Open `Task Status.txt`. Find the next `[ ]` uncompleted ticket.
3. **Read the Spec:** Open the corresponding file in the `Tickets/` directory (e.g., `Tickets/Ticket_1.1.md`). This file contains hyper-detailed, step-by-step instructions designed to fit within your context window. Follow it exactly.
4. **Execute:** Implement the feature as defined in the Ticket. Keep your code clean, modular, and following the Strategy Pattern.
4. **Compile & Verify:** The codebase must build properly (`dotnet build`) after every task. Never leave it in a broken state.
5. **Update Status:** Change `[ ]` to `[x]` in `Task Status.txt` for the completed task.
6. **Commit:** Commit the changes using git with a clear, small commit message (e.g., `git commit -m "Milestone 1, Ticket 1.1: Added IPlayableSong interface"`).
7. PR/Move on: Make pull requests if required, or simply move on to the next task in the file.

## Key Architecture Principles
* **No "Omni" God-Classes:** Keep Piano logic in `PianoSong` and Guitar logic in `GuitarSong`. The main loop should only know about `IPlayableSong`.
* **NAudio for Audio:** We rely on DryWetMIDI for MIDI events, but for `.psarc` audio, we use `NAudio` to load the decoded `.wav` files.
* **ImGui Native Rendering:** OmniRhythm relies on `ImGui.NET`. You will build the 3D highway using `drawList.AddQuadFilled`, `AddLine`, computing 3D perspective via pure math in C#. Do not use OpenGL directly.
