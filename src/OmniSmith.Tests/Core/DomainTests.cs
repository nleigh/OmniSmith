using Xunit;
using FluentAssertions;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Guitar;
using Moq;
using System;

namespace OmniSmith.Tests.Core;

public class DomainTests
{
    [Fact]
    public void GuitarSong_ShouldImplementIPlayableSong()
    {
        // Arrange
        var song = new GuitarSong();

        // Assert
        song.Should().BeAssignableTo<IPlayableSong>();
    }

    [Fact]
    public void GuitarSong_ShouldHaveDefaultValues()
    {
        // Arrange
        var song = new GuitarSong();

        // Assert
        song.Title.Should().Be("Unknown Title");
        song.Artist.Should().Be("Unknown Artist");
        song.TotalDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void MockSong_ShouldBeDisposable()
    {
        // Arrange
        var mockSong = new Mock<IPlayableSong>();
        
        // Act
        mockSong.Object.Dispose();

        // Assert
        mockSong.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void GuitarSong_InitAudio_ShouldNotThrow_WhenPathIsInvalid()
    {
        // Arrange
        var song = new GuitarSong();
        song.CachedWavPath = "C:\\invalid_audio_path_never_exists_123.wav";

        // Act
        var act = () => song.InitAudio();

        // Assert
        act.Should().NotThrow(); // Guard clause should simply return and not crash.
    }

    [Fact]
    public void GuitarSong_InitAudio_ShouldNotThrow_WhenPathIsEmpty()
    {
        // Arrange
        var song = new GuitarSong();
        song.CachedWavPath = "";

        // Act
        var act = () => song.InitAudio();

        // Assert
        act.Should().NotThrow();
    }
}
