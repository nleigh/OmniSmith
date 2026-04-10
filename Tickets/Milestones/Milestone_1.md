# Milestone 1: Core Engine Refactoring

## Overview
The goal of this milestone is to decouple the existing MIDI-specific rendering pipeline from the core application loop. By introducing the Strategy Pattern via the `IPlayableSong` interface, we can turn the central engine into an agnostic host environment. The legacy Openthesia logic will be encapsulated into a standalone "Piano Domain."

### 🎯 Key Objectives
- **Agnostic Core:** The `Application` host must not know about MIDI details.
- **Domain Isolation:** Wrap `MidiFileData` and the Veldrid/ImGui visual components deep inside `PianoSong`.
- **Seamless Transition:** The application should compile and flawlessly play `.mid` files precisely as before, but executing entirely through the `CurrentSong.Draw()` interface.

### 📝 Tickets
Please refer to the explicit step-by-step implementation specs in the `Tickets/` directory:
- [Ticket 1.1: Interface Extraction](../Tickets/Ticket_1.1.md)
- [Ticket 1.2: State Management & Main Loop](../Tickets/Ticket_1.2.md)
