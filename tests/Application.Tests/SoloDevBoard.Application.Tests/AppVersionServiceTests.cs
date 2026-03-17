using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="AppVersionService"/>.</summary>
public sealed class AppVersionServiceTests
{
    [Fact]
    public void Version_ValueRequested_ReturnsNonEmptyString()
    {
        // Arrange
        var sut = new AppVersionService();

        // Act
        var version = sut.Version;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public void UserAgent_ValueRequested_StartsWithAppNamePrefix()
    {
        // Arrange
        var sut = new AppVersionService();

        // Act
        var userAgent = sut.UserAgent;

        // Assert
        Assert.StartsWith("SoloDevBoard/", userAgent, StringComparison.Ordinal);
    }

    [Fact]
    public void UserAgent_ValueRequested_SuffixMatchesVersion()
    {
        // Arrange
        var sut = new AppVersionService();

        // Act
        var version = sut.Version;
        var userAgent = sut.UserAgent;

        // Assert
        Assert.Equal($"SoloDevBoard/{version}", userAgent);
    }
}
