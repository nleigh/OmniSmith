using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Rocksmith2014.XML;
using System.Text.RegularExpressions;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Core;
using OmniSmith.Core.Database;

namespace OmniSmith.Domains.Guitar.Services;

public static class RocksmithParser
{
    public static InstrumentalArrangement ParseXml(Stream stream)
    {
        // Many CDLC files contain null bytes, invalid characters, or structural malformations.
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);
        string xml = reader.ReadToEnd();
        
        // 1. Character-level sanitization (strip nulls/control chars)
        xml = new string(xml.Where(c => c == '\n' || c == '\r' || c == '\t' || (c >= 32 && c != 65534 && c != 65535)).ToArray());

        // 2. Structural healing (fix common attribute mistakes)
        xml = HealXml(xml);

        var arrangement = new InstrumentalArrangement();
        var settings = new XmlReaderSettings
        {
            CheckCharacters = false,
            IgnoreWhitespace = true,
            ValidationType = ValidationType.None
        };

        try
        {
            using var xmlReader = XmlReader.Create(new StringReader(xml), settings);
            xmlReader.MoveToContent();
            ((IXmlSerializable)arrangement).ReadXml(xmlReader);
            
            int totalNotes = arrangement.Levels.Sum(l => l.Notes.Count) + (arrangement.TranscriptionTrack?.Notes.Count ?? 0);
            Logger.Info($"RocksmithParser: XML Parsed. Title='{arrangement.MetaData?.Title}', Levels={arrangement.Levels.Count}, TotalNotes={totalNotes}");
            
            return arrangement;
        }
        catch (XmlException ex)
        {
            // Log a snippet around the failure point for analysis
            int pos = Math.Max(0, ex.LinePosition - 100);
            string snippet = "N/A";
            try 
            {
                // Find line content
                var lines = xml.Split('\n');
                if (ex.LineNumber > 0 && ex.LineNumber <= lines.Length)
                {
                    string targetLine = lines[ex.LineNumber - 1];
                    int start = Math.Max(0, ex.LinePosition - 50);
                    int length = Math.Min(100, targetLine.Length - start);
                    snippet = targetLine.Substring(start, length);
                }
            } catch { }

            Logger.Error($"RocksmithParser: XML Error at L{ex.LineNumber}, P{ex.LinePosition}. Snippet: ...{snippet}...");
            throw;
        }
    }

    private static string HealXml(string xml)
    {
        // Fix spaces around equals in attributes: attr = "val" -> attr="val"
        // Some CDLC tools generate XML with extra spaces that strict parsers dislike
        xml = Regex.Replace(xml, @"(\w+)\s+=\s*""", "$1=\"");
        xml = Regex.Replace(xml, @"(\w+)\s*=\s+""", "$1=\"");
        
        // Fix missing quotes in version or other header attributes if they occur
        // xml = Regex.Replace(xml, @"version=(\d+)", "version=\"$1\"");
        
        return xml;
    }

    public static GuitarSong ParseXmlToSong(Stream stream)
    {
        var arrangement = ParseXml(stream);
        Logger.Info($"RocksmithParser: Arrangement loaded. Levels: {arrangement.Levels.Count}, HasTranscription: {arrangement.TranscriptionTrack != null}");
        return MapArrangementToSong(arrangement);
    }

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
        Logger.Info($"RocksmithParser: Starting parse of '{psarcPath}'");

        var entries = PsarcReader.ExtractByFilter(psarcPath, name => 
            name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || 
            name.EndsWith(".wem", StringComparison.OrdinalIgnoreCase));

        var xmlEntryName = FindArrangementXml(entries.Keys);

        if (xmlEntryName == null)
        {
            Logger.Error($"RocksmithParser: No arrangement XML found inside {psarcPath}");
            throw new FileNotFoundException($"No arrangement XML found inside {psarcPath}");
        }

        Logger.Info($"RocksmithParser: Selecting arrangement XML: {xmlEntryName}");

        // Use Rocksmith2014.XML to load the arrangement from memory
        using var xmlStream = new MemoryStream(entries[xmlEntryName]);
        var arrangement = ParseXml(xmlStream);
        GuitarSong song = MapArrangementToSong(arrangement);

        // Find the WEM audio file and decode it
        var wemEntry = entries.Keys.FirstOrDefault(k => k.EndsWith(".wem", StringComparison.OrdinalIgnoreCase));
        if (wemEntry != null)
        {
            try
            {
                string cacheDir = Path.Combine(ProgramData.AppDir, "Cache", "Audio");
                string wavPath = WemDecoder.ConvertWemToWavAsync(entries[wemEntry], cacheDir).GetAwaiter().GetResult();
                song.CachedWavPath = wavPath;
            }
            catch (Exception ex)
            {
                Logger.Error($"RocksmithParser: Audio extraction failed for {psarcPath}", ex);
            }
        }

        return song;
    }

    public static SongMeta GetMetadata(string psarcPath)
    {
        try
        {
            var entries = PsarcReader.ExtractByFilter(psarcPath, name => name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            var xmlEntryName = FindArrangementXml(entries.Keys);

            if (xmlEntryName == null)
            {
                return new SongMeta(Path.GetFileNameWithoutExtension(psarcPath), "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
            }

            using var xmlStream = new MemoryStream(entries[xmlEntryName]);
            var arrangement = ParseXml(xmlStream);

            return new SongMeta(
                arrangement.MetaData.Title,
                arrangement.MetaData.ArtistName,
                arrangement.MetaData.AlbumName,
                arrangement.MetaData.AlbumYear.ToString(),
                0.0,
                arrangement.MetaData.Tuning?.ToString() ?? "Standard",
                "Lead",
                false
            );
        }
        catch (Exception ex)
        {
            Logger.Error($"RocksmithParser: Metadata error for {psarcPath}", ex);
            return new SongMeta(Path.GetFileNameWithoutExtension(psarcPath), "Unknown Artist", "Unknown Album", "Unknown Year", 0, "Standard", "Lead", false);
        }
    }

    private static GuitarSong MapArrangementToSong(InstrumentalArrangement arrangement)
    {
        GuitarSong song = new GuitarSong
        {
            Title = arrangement.MetaData.Title,
            Artist = arrangement.MetaData.ArtistName,
            TotalDuration = TimeSpan.FromSeconds(arrangement.MetaData.SongLength / 1000.0)
        };

        // Select the best level to use for the highway.
        // CDLC files often have empty transcription tracks or redundant levels.
        // We pick the level (or transcription track) that has the most combined notes and chords.
        var allPossibleLevels = arrangement.Levels.ToList();
        if (arrangement.TranscriptionTrack != null) allPossibleLevels.Add(arrangement.TranscriptionTrack);

        var level = allPossibleLevels
            .OrderByDescending(l => l.Notes.Count + l.Chords.Count)
            .FirstOrDefault();

        if (level == null) return song;

        // Map Notes
        foreach (var n in level.Notes)
        {
            song.Notes.Add(new GuitarNote
            {
                Time = n.Time / 1000.0f,
                String = (int)n.String,
                Fret = (int)n.Fret,
                Duration = n.Sustain / 1000.0f,
                Techniques = MapTechniques(n),
                BendValue = n.MaxBend,
                SlideTo = n.SlideTo
            });
        }

        // Map Chords
        foreach (var c in level.Chords)
        {
            var chordNotes = new List<GuitarNote>();
            if (c.ChordNotes != null)
            {
                foreach (var cn in c.ChordNotes)
                {
                    chordNotes.Add(new GuitarNote
                    {
                        Time = cn.Time / 1000.0f,
                        String = (int)cn.String,
                        Fret = (int)cn.Fret,
                        Duration = cn.Sustain / 1000.0f,
                        Techniques = MapTechniques(cn)
                    });
                }
            }

            song.Chords.Add(new GuitarChord
            {
                Time = c.Time / 1000.0f,
                ChordId = c.ChordId,
                ChordNotes = chordNotes,
                Name = arrangement.ChordTemplates[c.ChordId].Name
            });
        }

        // Map Anchors and Beats
        song.Anchors = level.Anchors.Select(a => a.Time / 1000.0f).ToList();
        song.Beats = arrangement.Ebeats.Select(b => b.Time / 1000.0f).ToList();

        return song;
    }

    private static NoteTechnique MapTechniques(Rocksmith2014.XML.Note n)
    {
        NoteTechnique t = NoteTechnique.None;
        if (n.IsBend || n.MaxBend > 0) t |= NoteTechnique.Bend;
        if (n.IsSlide) t |= NoteTechnique.Slide;
        if (n.IsUnpitchedSlide) t |= NoteTechnique.UnpitchedSlide;
        if (n.IsHammerOn) t |= NoteTechnique.HammerOn;
        if (n.IsPullOff) t |= NoteTechnique.PullOff;
        if (n.IsHarmonic) t |= NoteTechnique.Harmonic;
        if (n.IsPinchHarmonic) t |= NoteTechnique.PinchHarmonic;
        if (n.IsPalmMute) t |= NoteTechnique.PalmMute;
        if (n.IsFretHandMute) t |= NoteTechnique.Mute;
        if (n.IsTremolo) t |= NoteTechnique.Tremolo;
        return t;
    }
}
