using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Core.Interfaces;

namespace OmniSmith.Domains.Guitar.Services;

public static class RocksmithParser
{
    public static GuitarSong ParsePsarc(string psarcPath)
    {
        // For the purpose of Ticket 2.2, we implement a mocked extraction
        // that looks for a '_lead.xml' file in the same directory as the PSARC.
        // In a full implementation, this would use a PSARC extractor.
        string xmlPath = psarcPath.Replace(".psarc", "_lead.xml");
        
        if (!System.IO.File.Exists(xmlPath))
        {
            throw new System.IO.FileNotFoundException($"Could not find arrangement XML at {xmlPath}. Please provide a raw _lead.xml for mocking.");
        }

        return ParseXml(xmlPath);
    }

    public static OmniSmith.Core.Database.SongMeta GetMetadata(string psarcPath)
    {
        string xmlPath = psarcPath.Replace(".psarc", "_lead.xml");
        if (!System.IO.File.Exists(xmlPath))
        {
            return new OmniSmith.Core.Database.SongMeta(
                System.IO.Path.GetFileNameWithoutExtension(psarcPath),
                "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
        }

        try
        {
            XDocument doc = XDocument.Load(xmlPath);
            XElement root = doc.Root ?? throw new InvalidOperationException();

            return new OmniSmith.Core.Database.SongMeta(
                root.Attribute("title")?.Value ?? System.IO.Path.GetFileNameWithoutExtension(psarcPath),
                root.Attribute("artist")?.Value ?? "Unknown Artist",
                root.Attribute("album")?.Value ?? "Unknown Album",
                root.Attribute("year")?.Value ?? "Unknown Year",
                0.0, // Duration would require parsing all notes
                root.Attribute("tuning")?.Value ?? "Standard",
                "Lead",
                false
            );
        }
        catch
        {
            return new OmniSmith.Core.Database.SongMeta(
                System.IO.Path.GetFileNameWithoutExtension(psarcPath),
                "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
        }
    }

    public static GuitarSong ParseXml(string xmlPath)
    {
        XDocument doc = XDocument.Load(xmlPath);
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
                        ChordNotes = chordNotes
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
            BendValue = _Float(n, "bend"),
            SlideTo = _Int(n, "slideTo", -1)
        };
    }

    private static float _Float(XElement el, string attr, float def = 0.0f) => 
        float.TryParse(el.Attribute(attr)?.Value, out float res) ? res : def;

    private static int _Int(XElement el, string attr, int def = 0) => 
        int.TryParse(el.Attribute(attr)?.Value, out int res) ? res : def;

    private static bool _Bool(XElement el, string attr) => 
        el.Attribute(attr)?.Value != null && el.Attribute(attr).Value != "0";
}

public class ChordTemplate
{
    public string Name { get; set; } = string.Empty;
    public List<int> Fingers { get; set; } = new();
    public List<int> Frets { get; set; } = new();
}
