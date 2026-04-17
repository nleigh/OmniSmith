# Ticket 4.1: File Browser Update

## Goal
Expand OmniSmith's OpenFileDialog (and OS drag-and-drop ingestion) to allow `.psarc` files alongside `.mid`.

## Instructions
1. **Update `src\OmniSmith\Core\FileDialogs\OpenFileDialog.cs`**:
   * Add `*.psarc` to the allowed extension filters so the Windows dialog displays them.
   * `Filter = "Playable Files (*.mid, *.psarc)|*.mid;*.psarc|All Files (*.*)|*.*"`
2. **Update `MidiBrowserWindow.cs`**:
   * Specifically in the `DrawDirectoryFiles` logic, verify that `.psarc` files are enumerated and rendered as clickable items.
3. **Verify**:
   Run the application, navigate to a directory with a Rocksmith 2014 CDLC file, and verify it appears in the ImGui browser.


### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.
