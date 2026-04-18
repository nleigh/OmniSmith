# [COMPLETED] Milestone 4: Integration & Domain Routing

## Overview
With both the Piano and Guitar domains fully functional in isolation, the final milestone bridges the runtime gap. We must allow the user to drag and drop both `.mid` and `.psarc` files into the `OmniSmith` UI and seamlessly route them to the correct Domain parser on the fly.

### 🎯 Key Objectives
- **File Ingestion:** Expand the ImGui file browser and OS drag-and-drop callbacks to accept the `.psarc` extension.
- **Asynchronous Routing:** Implement a non-blocking `SongFactory` that extracts heavily-compressed Wwise audio on a background thread without freezing the main application frame.
- **UI UX:** Provide informative "Extracting..." loading screens to the user while transitioning between songs.

### 📝 Tickets
File modification procedures and async C# logic are documented here:
- [Ticket 4.1: File Browser Update](../Ticket_4.1.md)
- [Ticket 4.2: Asynchronous Song Factory](../Ticket_4.2.md)
