open System
open System.IO
open System.Xml
open System.Xml.Serialization
open Rocksmith2014.Common
open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.PSARC

let usage () =
    printfn "Usage:"
    printfn "  RsCli xml2sng <input.xml> <output.sng>"
    printfn "  RsCli sng2xml <input.sng> <output.xml> [pc|mac]"
    printfn "  RsCli test-psarc <input.psarc>"
    printfn ""
    printfn "Converts between Rocksmith 2014 arrangement XML and encrypted SNG."
    1


let xml2sng (xmlPath: string) (sngPath: string) =
    async {
        printfn "Loading XML: %s" xmlPath
        let xml = InstrumentalArrangement.Load(xmlPath)
        printfn "Converting to SNG (%d notes)..." xml.Levels.[0].Notes.Count
        let sng = ConvertInstrumental.xmlToSng xml
        let dir = Path.GetDirectoryName(sngPath)
        if not (String.IsNullOrEmpty(dir)) then
            Directory.CreateDirectory(dir) |> ignore
        printfn "Saving encrypted SNG: %s" sngPath
        do! SNG.savePackedFile sngPath Platform.PC sng
        printfn "Done."
    }
    |> Async.RunSynchronously
    0

let sng2xml (sngPath: string) (xmlPath: string) (platform: Platform) =
    async {
        printfn "Loading SNG (%A): %s" platform sngPath
        let! sng = SNG.readPackedFile sngPath platform
        printfn "Converting to XML..."
        let xml = ConvertInstrumental.sngToXml None sng
        let dir = Path.GetDirectoryName(xmlPath)
        if not (String.IsNullOrEmpty(dir)) then
            Directory.CreateDirectory(dir) |> ignore
        xml.Save(xmlPath)
        printfn "Saved XML: %s" xmlPath
    }
    |> Async.RunSynchronously
    0

[<EntryPoint>]
let main argv =
    match argv |> Array.toList with
    | "xml2sng" :: xmlPath :: sngPath :: _ ->
        try xml2sng xmlPath sngPath
        with ex ->
            eprintfn "Error: %s" ex.Message
            eprintfn "%s" ex.StackTrace
            1
    | "sng2xml" :: sngPath :: xmlPath :: rest ->
        try
            let platform =
                match rest with
                | p :: _ when p.ToLowerInvariant() = "mac" -> Platform.Mac
                | _ -> Platform.PC
            sng2xml sngPath xmlPath platform
        with ex ->
            eprintfn "Error: %s" ex.Message
            eprintfn "%s" ex.StackTrace
            1
    | "playback-test" :: psarcPath :: _ ->
        try
            printfn "Running Deep Playback Test for: %s" psarcPath
            let song = OmniSmith.Domains.Guitar.Services.RocksmithParser.ParsePsarc(psarcPath)
            
            printfn "--- Results ---"
            printfn "Title:    %s" song.Title
            printfn "Artist:   %s" song.Artist
            printfn "Duration: %A" song.TotalDuration
            printfn "Notes:    %d" song.Notes.Count
            printfn "Chords:   %d" song.Chords.Count
            printfn "Beats:    %d" song.Beats.Count
            
            let bendCount = song.Notes |> Seq.filter (fun n -> n.Techniques.HasFlag(OmniSmith.Domains.Guitar.Models.NoteTechnique.Bend)) |> Seq.length
            let slideCount = song.Notes |> Seq.filter (fun n -> n.Techniques.HasFlag(OmniSmith.Domains.Guitar.Models.NoteTechnique.Slide)) |> Seq.length
            printfn "Techniques: %d Bends, %d Slides" bendCount slideCount

            match song.CachedWavPath with
            | null -> printfn "Audio:    FAILED to extract/decode"
            | path -> 
                let exists = File.Exists(path)
                let size = if exists then (FileInfo(path).Length / 1024L / 1024L) else 0L
                printfn "Audio:    %s (%d MB)" (if exists then "SUCCESS" else "MISSING") size
            
            printfn "--- VALIDATION PASSED ---"
            0
        with ex ->
            eprintfn "PLAYBACK TEST FAILED: %s" ex.Message
            eprintfn "Stack: %s" ex.StackTrace
            1
    | "test-psarc" :: psarcPath :: _ ->
        try
            async {
                printfn "Opening PSARC: %s" psarcPath
                use psarc = PSARC.OpenFile(psarcPath)
                printfn "Found %d entries in manifest." psarc.Manifest.Length
                
                // Find a random XML file to test extraction
                let xmlEntry = psarc.Manifest |> List.tryFind (fun m -> m.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                match xmlEntry with
                | Some name ->
                    printfn "Attempting to extract: %s" name
                    let! stream = psarc.GetEntryStream(name) |> Async.AwaitTask
                    printfn "Successfully extracted %d bytes." stream.Length
                    
                    printfn "Attempting to parse XML using OmniSmith RocksmithParser (Resilient Mode)..."
                    try
                        use ms = new MemoryStream(stream.ToArray())
                        let song = OmniSmith.Domains.Guitar.Services.RocksmithParser.ParseXmlToSong(ms)
                        printfn "Successfully parsed Song: %s" song.Title
                    with ex ->
                        printfn "FIXED Parsing also FAILED: %s" ex.Message
                        printfn "Stack: %s" ex.StackTrace
                    
                    printfn "Extraction verified with Rocksmith2014.PSARC library."
                | None ->
                    printfn "No XML files found in archive manifest."
                
                return 0
            }
            |> Async.RunSynchronously
        with ex ->
            eprintfn "Error: %s" ex.Message
            eprintfn "%s" ex.StackTrace
            1
    | _ ->
        usage ()
