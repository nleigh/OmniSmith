# Milestone 2: The Guitar Domain & CDLC Parsing

## Overview
With the Openthesia engine refactored into `OmniSmith`, we introduce the **Guitar Domain**. This milestone revolves around building the C# models necessary to represent Rocksmith arrangements and porting the parsing logic from Slopsmith's Python backend into our C# ecosystem.

### 🎯 Key Objectives
- **Data Structures:** Implement Rocksmith models (`GuitarNote`, `GuitarChord`, `NoteTechnique` flags).
- **XML Parsing:** Deserialize `.psarc` internal metadata (`_lead.xml`) to extract frets, sustains, and beats.
- **Audio Extraction:** Convert Wwise `.wem` audio payloads into generic formats (WAV) utilizing `vgmstream-cli` for playback via NAudio (with planned migration to ManagedBass).

### 📝 Tickets
All detailed logic, API usages, and structures live in the `Tickets/` directory:
- [Ticket 2.1: Domain Models](../Ticket_2.1.md)
- [Ticket 2.2: Rocksmith CDLC Parsing](../Ticket_2.2.md)
- [Ticket 2.3: Audio Extraction Pipeline](../Ticket_2.3.md)
