# Ticket 2.2: Rocksmith CDLC Parsing

## Goal
Implement the system that unzips `.psarc` files, finds the XML arrangement, and parses it into the models from Ticket 2.1.

## Instructions
1. **Dependency Addition**:
   * We will leverage `Rocksmith2014.XML` (or parsing System.Xml manually if not linking F# assemblies). 
   * A recommended approach is to just use standard `System.Xml.Linq` (XDocument) to parse the `Arrangement` tree.
2. **Create `RocksmithParser.cs` in `Domains/Guitar/Services`**:
   * Expose `public static GuitarSong ParsePsarc(string psarcPath)`.
   * **Step 1:** To unpack PSARC, use standard logic. The file header is `PSAR`. If writing a custom parser is too complex, you can look for `RocksmithToolkitLib.PSARC` via a local build or include. If not possible, for this task, write a mocked parser that takes a raw `_lead.xml` file.
   * **Step 2 (XML Parsing):**
     * **[LOCAL AGENT HELP:]** Refer to this Python syntax from Slopsmith's `song.py` to understand the XML tree:
       ```python
       for note_elem in root.findall(".//notes/note"):
           time = float(note_elem.get("time"))
           string = int(note_elem.get("string"))
           fret = int(note_elem.get("fret"))
           sustain = float(note_elem.get("sustain"))
           # techniques: bend, slideTo, unpitchedSlide, hammerOn, pullOff
       ```
     * Using `XDocument.Load(xmlStream)`, implement the equivalent C# logic to populate your `GuitarNote` models map to `NoteTechnique` flags.
     - Read `chords > chord`. Map to `GuitarChord`. Find its `chordId` and match against `chordTemplates > chordTemplate` to get the fingerings/frets, converting them into a list of `GuitarNote` inside the `GuitarChord`.
     - Read `ebeats > ebeat` and extract `time` to the beats list.
3. **Integration**:
   * Have `RocksmithParser` return a fully populated `GuitarSong`.


### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.
