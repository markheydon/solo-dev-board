using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="AppVersionBuiltAtFormatter"/>.</summary>
public sealed class AppVersionBuiltAtFormatterTests
{
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.3.4")]
    public void FormatDisplay_ReleaseVersion_ReturnsEmptyString(string version)
    {
        var buildTimestampUtc = new DateTimeOffset(2026, 8, 23, 14, 11, 0, TimeSpan.Zero);

        var display = AppVersionBuiltAtFormatter.FormatDisplay(version, buildTimestampUtc);

        Assert.Equal(string.Empty, display);
    }

    [Fact]
    public void FormatDisplay_PreReleaseVersionWithoutTimestamp_ReturnsEmptyString()
    {
        var display = AppVersionBuiltAtFormatter.FormatDisplay("1.0.1-staging.0.49", null);

        Assert.Equal(string.Empty, display);
    }

    [Fact]
    public void FormatDisplay_PreReleaseVersionWithSummerTimestamp_ReturnsUkLocalisedDisplay()
    {
        var buildTimestampUtc = new DateTimeOffset(2026, 8, 23, 14, 11, 0, TimeSpan.Zero);

        var display = AppVersionBuiltAtFormatter.FormatDisplay("1.0.1-staging.0.49", buildTimestampUtc);

        Assert.Equal("23 Aug 26 @ 15:11 BST", display);
    }

    [Fact]
    public void FormatDisplay_PreReleaseVersionWithWinterTimestamp_ReturnsGmtAbbreviation()
    {
        var buildTimestampUtc = new DateTimeOffset(2026, 1, 15, 12, 30, 0, TimeSpan.Zero);

        var display = AppVersionBuiltAtFormatter.FormatDisplay("1.0.1-staging.0.12", buildTimestampUtc);

        Assert.Equal("15 Jan 26 @ 12:30 GMT", display);
    }
}
