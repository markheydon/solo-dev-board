using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for hosted authentication error route helpers.</summary>
public sealed class HostedAuthErrorRoutesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildErrorUrl_InvalidReason_ThrowsArgumentException(string? reason)
    {
        // Act
        var act = () => HostedAuthErrorRoutes.BuildErrorUrl(reason!);

        // Assert
        var exception = Assert.ThrowsAny<ArgumentException>(act);
        Assert.Equal("reason", exception.ParamName);
    }

    [Fact]
    public void BuildErrorUrl_ValidReason_ReturnsEncodedUrl()
    {
        // Act
        var url = HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SessionExpired, "/repositories");

        // Assert
        Assert.Contains("reason=session-expired", url, StringComparison.Ordinal);
        Assert.Contains("returnUrl=%2Frepositories", url, StringComparison.Ordinal);
    }
}
