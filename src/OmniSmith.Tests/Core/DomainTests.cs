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
}
