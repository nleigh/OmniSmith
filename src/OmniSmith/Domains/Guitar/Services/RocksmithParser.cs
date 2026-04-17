using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Core.Interfaces;

namespace OmniSmith.Domains.Guitar.Services;

public static class RocksmithParser
{
    /// <summary>
    /// Selects the best arrangement XML entry name from a collection of filenames.
    /// Prefers Lead, then Combo, filtering out vocals/showlights/tones.
    /// </summary>
    public static string? FindArrangementXml(IEnumerable<string> filenames)
    {
        return filenames
            .Where(k => k.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Where(k => !k.Contains("vocal", StringComparison.OrdinalIgnoreCase)
                     && !k.Contains("showlight", StringComparison.OrdinalIgnoreCase)
                     && !k.Contains("tone", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(k => k.Contains("lead", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(k => k.Contains("combo", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    public static GuitarSong ParsePsarc(string psarcPath)
    {
        // Extract all files from the PSARC archive
        var entries = PsarcReader.ExtractAll(psarcPath);

        // Find the lead arrangement XML
        var xmlEntry = FindArrangementXml(entries.Keys);

        if (xmlEntry == null)
            throw new FileNotFoundException($"No arrangement XML found inside {psarcPath}");

        // Parse the XML from memory
        using var xmlStream = new MemoryStream(entries[xmlEntry]);
        var song = ParseXml(xmlStream);

        // Find the WEM audio file and decode it
        var wemEntry = entries.Keys
            .FirstOrDefault(k => k.EndsWith(".wem", StringComparison.OrdinalIgnoreCase));

        if (wemEntry != null)
        {
            try
            {
                string cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OmniSmith", "Cache", "Audio");

                string wavPath = WemDecoder.ConvertWemToWavAsync(entries[wemEntry], cacheDir).GetAwaiter().GetResult();
                song.CachedWavPath = wavPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Audio extraction failed for {psarcPath}: {ex.Message}");
                // Song will still work visually, just without audio
            }
        }

        return song;
    }

    public static OmniSmith.Core.Database.SongMeta GetMetadata(string psarcPath)
    {
        try
        {
            // Only extract XML files — skip audio/images to keep scanning fast
            var entries = PsarcReader.ExtractByFilter(psarcPath,
                name => name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            var xmlEntry = FindArrangementXml(entries.Keys);

            if (xmlEntry == null)
            {
                return new OmniSmith.Core.Database.SongMeta(
                    Path.GetFileNameWithoutExtension(psarcPath),
                    "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
            }

            using var xmlStream = new MemoryStream(entries[xmlEntry]);
            return GetMetadataFromXml(xmlStream, psarcPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing metadata for {psarcPath}: {ex.Message}");
            return new OmniSmith.Core.Database.SongMeta(
                Path.GetFileNameWithoutExtension(psarcPath),
                "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
        }
    }

    /// <summary>
    /// Extracts metadata (title, artist, album, etc.) from a Rocksmith arrangement XML stream.
    /// </summary>
    public static OmniSmith.Core.Database.SongMeta GetMetadataFromXml(Stream xmlStream, string fallbackName = "Unknown")
    {
        XDocument doc = XDocument.Load(xmlStream);
        XElement root = doc.Root ?? throw new InvalidOperationException("Invalid XML: no root element");

        return new OmniSmith.Core.Database.SongMeta(
            root.Attribute("title")?.Value ?? Path.GetFileNameWithoutExtension(fallbackName),
            root.Attribute("artist")?.Value ?? "Unknown Artist",
            root.Attribute("album")?.Value ?? "Unknown Album",
            root.Attribute("year")?.Value ?? "Unknown Year",
            0.0,
            root.Attribute("tuning")?.Value ?? "Standard",
            "Lead",
            false
        );
    }

    public static GuitarSong ParseXml(string xmlPath)
    {
        using var fs = File.OpenRead(xmlPath);
        return ParseXml(fs);
    }

    public static GuitarSong ParseXml(Stream xmlStream)
    {
        XDocument doc = XDocument.Load(xmlStream);
        XElement root = doc.Root ?? throw new InvalidOperationException("Invalid XML: Root element not found.");

        GuitarSong song = new GuitarSong();

        // 1. Parse Chord Templates
        var chordTemplates = new List<ChordTemplate>();
        var templatesContainer = root.Element("chordTemplates");
        if (templatesContainer != null)
        {
            foreach (var ct in templatesContainer.Elements("chordTemplate"))
            {
                chordTemplates.Add(new ChordTemplate
                {
                    Name = ct.Attribute("chordName")?.Value ?? string.Empty,
                    Fingers = Enumerable.Range(0, 6).Select(i => _Int(ct, $"finger{i}", -1)).ToList(),
                    Frets = Enumerable.Range(0, 6).Select(i => _Int(ct, $"fret{i}", -1)).ToList()
                });
            }
        }

        // 2. Handle Levels and Phrases (The "Slopsmith Merge" logic)
        var levelsEl = root.Element("levels");
        var phrasesEl = root.Element("phrases");
        var phraseItersEl = root.Element("phraseIterations");

        var allLevels = new Dictionary<int, XElement>();
        if (levelsEl != null)
        {
            foreach (var level in levelsEl.Elements("level"))
            {
                allLevels[_Int(level, "difficulty")] = level;
            }
        }

        List<GuitarNote> allNotes = new();
        List<GuitarChord> allChords = new();
        List<float> allAnchors = new();

        if (allLevels.Count == 1)
        {
            CollectFromLevel(allLevels.Values.First(), 0.0f, float.MaxValue, allNotes, allChords, allAnchors, chordTemplates);
        }
        else if (phrasesEl != null && phraseItersEl != null && allLevels.Count > 0)
        {
            var phrases = phrasesEl.Elements("phrase").ToList();
            var iterations = phraseItersEl.Elements("phraseIteration").ToList();

            for (int i = 0; i < iterations.Count; i++)
            {
                int pid = _Int(iterations[i], "phraseId");
                if (pid >= phrases.Count) continue;

                int maxDiff = _Int(phrases[pid], "maxDifficulty");
                if (!allLevels.TryGetValue(maxDiff, out var level))
                {
                    // Fallback to closest lower level
                    level = allLevels.Where(kvp => kvp.Key <= maxDiff).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Value).FirstOrDefault();
                }

                if (level == null) continue;

                float tStart = _Float(iterations[i], "time");
                float tEnd = (i + 1 < iterations.Count) ? _Float(iterations[i + 1], "time") : float.MaxValue;

                CollectFromLevel(level, tStart, tEnd, allNotes, allChords, allAnchors, chordTemplates);
            }
        }
        else if (allLevels.Count > 0)
        {
            // Fallback: use level with most notes
            var bestLevel = allLevels.Values.OrderByDescending(lv => 
                (lv.Element("notes")?.Attribute("count") != null ? _Int(lv.Element("notes"), "count") : 0) +
                (lv.Element("chords")?.Attribute("count") != null ? _Int(lv.Element("chords"), "count") : 0)
            ).First();
            CollectFromLevel(bestLevel, 0.0f, float.MaxValue, allNotes, allChords, allAnchors, chordTemplates);
        }

        song.Notes = allNotes.OrderBy(n => n.Time).ToList();
        song.Chords = allChords.OrderBy(c => c.Time).ToList();
        song.Anchors = allAnchors.OrderBy(a => a).ToList();

        // 3. Parse Beats (ebeats)
        var beatsContainer = root.Element("ebeats");
        if (beatsContainer != null)
        {
            foreach (var eb in beatsContainer.Elements("ebeat"))
            {
                song.Beats.Add(_Float(eb, "time"));
            }
        }

        return song;
    }

    private static void CollectFromLevel(XElement level, float tStart, float tEnd, List<GuitarNote> notes, List<GuitarChord> chords, List<float> anchors, List<ChordTemplate> templates)
    {
        var notesContainer = level.Element("notes");
        if (notesContainer != null)
        {
            foreach (var n in notesContainer.Elements("note"))
            {
                float t = _Float(n, "time");
                if (t >= tStart && t < tEnd)
                {
                    notes.Add(ParseNote(n));
                }
            }
        }

        var chordsContainer = level.Element("chords");
        if (chordsContainer != null)
        {
            foreach (var c in chordsContainer.Elements("chord"))
            {
                float t = _Float(c, "time");
                if (t >= tStart && t < tEnd)
                {
                    var chordNotes = c.Elements("chordNote").Select(ParseNote).ToList();
                    int cid = _Int(c, "chordId");

                    if (chordNotes.Count == 0 && cid >= 0 && cid < templates.Count)
                    {
                        var ct = templates[cid];
                        for (int s = 0; s < 6; s++)
                        {
                            if (ct.Frets[s] >= 0)
                            {
                                chordNotes.Add(new GuitarNote { Time = t, String = s, Fret = ct.Frets[s] });
                            }
                        }
                    }

                    chords.Add(new GuitarChord
                    {
                        Time = t,
                        ChordId = cid,
                        ChordNotes = chordNotes,
                        Name = cid >= 0 && cid < templates.Count ? templates[cid].Name : string.Empty
                    });
                }
            }
        }

        var anchorsContainer = level.Element("anchors");
        if (anchorsContainer != null)
        {
            foreach (var a in anchorsContainer.Elements("anchor"))
            {
                float t = _Float(a, "time");
                if (t >= tStart && t < tEnd)
                {
                    anchors.Add(t);
                }
            }
        }
    }

    private static GuitarNote ParseNote(XElement n)
    {
        var techniques = new NoteTechnique();
        if (_Bool(n, "bend")) techniques |= NoteTechnique.Bend;
        if (_Bool(n, "slideTo")) techniques |= NoteTechnique.Slide;
        if (_Bool(n, "slideUnpitchTo")) techniques |= NoteTechnique.UnpitchedSlide;
        if (_Bool(n, "hammerOn")) techniques |= NoteTechnique.HammerOn;
        if (_Bool(n, "pullOff")) techniques |= NoteTechnique.PullOff;
        if (_Bool(n, "harmonic")) techniques |= NoteTechnique.Harmonic;
        if (_Bool(n, "harmonicPinch")) techniques |= NoteTechnique.PinchHarmonic;
        if (_Bool(n, "palmMute")) techniques |= NoteTechnique.PalmMute;
        if (_Bool(n, "mute")) techniques |= NoteTechnique.Mute;
        if (_Bool(n, "tremolo")) techniques |= NoteTechnique.Tremolo;

        return new GuitarNote
        {
            Time = _Float(n, "time"),
            String = _Int(n, "string"),
            Fret = _Int(n, "fret"),
            Duration = _Float(n, "sustain"),
            Techniques = techniques,
            BendValue = _Float(n, "bendValue", _Float(n, "bend")),
            SlideTo = _Int(n, "slideTo", -1)
        };
    }

    private static float _Float(XElement el, string attr, float def = 0.0f) => 
        float.TryParse(el.Attribute(attr)?.Value, out float res) ? res : def;

    private static int _Int(XElement el, string attr, int def = 0) => 
        int.TryParse(el.Attribute(attr)?.Value, out int res) ? res : def;

    private static bool _Bool(XElement el, string attr)
    {
        var val = el.Attribute(attr)?.Value;
        if (string.IsNullOrEmpty(val)) return false;
        if (val == "0" || val == "0.0") return false;
        if (float.TryParse(val, out float f) && f == 0.0f) return false;
        return true;
    }
}

public class ChordTemplate
{
    public string Name { get; set; } = string.Empty;
    public List<int> Fingers { get; set; } = new();
    public List<int> Frets { get; set; } = new();
}
