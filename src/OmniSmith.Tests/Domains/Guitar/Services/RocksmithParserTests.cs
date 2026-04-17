using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using OmniSmith.Domains.Guitar.Models;
using OmniSmith.Domains.Guitar.Services;
using Xunit;
using FluentAssertions;

namespace OmniSmith.Tests.Domains.Guitar.Services;

public class RocksmithParserTests : IDisposable
{
    private readonly string _tempXmlPath;

    public RocksmithParserTests()
    {
        _tempXmlPath = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempXmlPath))
        {
            File.Delete(_tempXmlPath);
        }
    }

    private void CreateTestXml(string content)
    {
        File.WriteAllText(_tempXmlPath, content);
    }

    [Fact]
    public void ParseXml_ShouldCorrectlyParseNotesAndTechniques()
    {
        // Arrange
        string xml = @"
        <song>
            <levels>
                <level difficulty='0'>
                    <notes count='2'>
                        <note time='1.0' string='0' fret='5' sustain='0.1' bend='1' bendValue='0.5' />
                        <note time='2.0' string='1' fret='7' sustain='0.1' slideTo='3' />
                    </notes>
                </level>
            </levels>
        </song>";
        CreateTestXml(xml);

        // Act
        var song = RocksmithParser.ParseXml(_tempXmlPath);

        // Assert
        song.Notes.Should().HaveCount(2);
        
        var note1 = song.Notes[0];
        note1.Time.Should().Be(1.0f);
        note1.Techniques.Should().HaveFlag(NoteTechnique.Bend);
        note1.BendValue.Should().Be(0.5f);

        var note2 = song.Notes[1];
        note2.Techniques.Should().HaveFlag(NoteTechnique.Slide);
        note2.SlideTo.Should().Be(3);
    }

    [Fact]
    public void ParseXml_ShouldMergeDifficultyCorrectly()
    {
        // Arrange
        string xml = @"
        <song>
            <levels>
                <level difficulty='0'>
                    <notes count='1'><note time='1.0' string='0' fret='1' sustain='0.1' /></notes>
                </level>
                <level difficulty='1'>
                    <notes count='1'><note time='1.0' string='0' fret='5' sustain='0.1' /></notes>
                </level>
            </levels>
            <phrases>
                <phrase phraseId='0' maxDifficulty='1' />
            </phrases>
            <phraseIterations>
                <phraseIteration phraseId='0' time='0.0' />
            </phraseIterations>
        </song>";
        CreateTestXml(xml);

        // Act
        var song = RocksmithParser.ParseXml(_tempXmlPath);

        // Assert
        song.Notes.Should().HaveCount(1);
        song.Notes[0].Fret.Should().Be(5); // Should take difficulty 1
    }

    [Fact]
    public void ParseXml_ShouldPopulateChordNames()
    {
        // Arrange
        string xml = @"
        <song>
            <chordTemplates>
                <chordTemplate chordName='G Major' fret0='3' fret1='2' fret2='0' fret3='0' fret4='3' fret5='3' />
            </chordTemplates>
            <levels>
                <level difficulty='0'>
                    <chords count='1'>
                        <chord time='1.0' chordId='0' />
                    </chords>
                </level>
            </levels>
        </song>";
        CreateTestXml(xml);

        // Act
        var song = RocksmithParser.ParseXml(_tempXmlPath);

        // Assert
        song.Chords.Should().HaveCount(1);
        song.Chords[0].Name.Should().Be("G Major");
    }

    [Fact]
    public void BoolParsing_ShouldHandleFloatStringsAsFalse()
    {
        // Arrange
        string xml = @"
        <song>
            <levels>
                <level difficulty='0'>
                    <notes count='1'>
                        <note time='1.0' string='0' fret='1' sustain='0.1' bend='0.0' />
                    </notes>
                </level>
            </levels>
        </song>";
        CreateTestXml(xml);

        // Act
        var song = RocksmithParser.ParseXml(_tempXmlPath);

        // Assert
        song.Notes[0].Techniques.Should().NotHaveFlag(NoteTechnique.Bend);
    }

    [Fact]
    public void GetMetadata_ThrowsMissingXml_ReturnsDefault()
    {
        var meta = RocksmithParser.GetMetadata("nonexistent.psarc");
        meta.Title.Should().Be("nonexistent");
        meta.Artist.Should().Be("Unknown Artist");
    }

    [Fact]
    public void GetMetadata_ParsesBasicAttributes()
    {
        string psarcPath = Path.Combine(Path.GetTempPath(), "testsong.psarc");
        string xmlPath = psarcPath.Replace(".psarc", "_lead.xml");
        string xml = @"<song title='TestTitle' artist='TestArtist' album='TestAlbum' year='2020' tuning='Drop D' />";
        File.WriteAllText(xmlPath, xml);

        try
        {
            var meta = RocksmithParser.GetMetadata(psarcPath);
            
            meta.Title.Should().Be("TestTitle");
            meta.Artist.Should().Be("TestArtist");
            meta.Album.Should().Be("TestAlbum");
            meta.Year.Should().Be("2020");
            meta.Tuning.Should().Be("Drop D");
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }
}
