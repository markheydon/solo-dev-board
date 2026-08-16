using Microsoft.AspNetCore.Http;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

public sealed class HostedAuthErrorPresentationTests
{
    [Theory]
    [InlineData(HostedAuthErrorRoutes.AccessDenied, "Access denied")]
    [InlineData(HostedAuthErrorRoutes.SignInDenied, "Sign-in cancelled")]
    [InlineData(HostedAuthErrorRoutes.SignInStateInvalid, "Sign-in session expired")]
    [InlineData(HostedAuthErrorRoutes.SignInIncomplete, "Sign-in incomplete")]
    [InlineData(HostedAuthErrorRoutes.SignInFailed, "Sign-in failed")]
    [InlineData(HostedAuthErrorRoutes.SignInUnavailable, "GitHub unavailable")]
    [InlineData(HostedAuthErrorRoutes.SignInUnknown, "Sign-in failed")]
    [InlineData(HostedAuthErrorRoutes.SessionExpired, "Session expired")]
    public void Resolve_KnownReason_ReturnsExpectedTitle(string reason, string expectedTitle)
    {
        // Act
        var presentation = HostedAuthErrorPresentationMapper.Resolve(reason);

        // Assert
        Assert.Equal(expectedTitle, presentation.Title);
        Assert.False(string.IsNullOrWhiteSpace(presentation.Message));
        Assert.True(presentation.StatusCode >= StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Resolve_UnknownReason_ReturnsFallbackPresentation()
    {
        // Act
        var presentation = HostedAuthErrorPresentationMapper.Resolve("not-a-real-reason");

        // Assert
        Assert.Equal(
            HostedAuthErrorPresentationMapper.Resolve(HostedAuthErrorRoutes.SignInUnknown).Title,
            presentation.Title);
        Assert.Equal(
            HostedAuthErrorPresentationMapper.Resolve(HostedAuthErrorRoutes.SignInUnknown).Message,
            presentation.Message);
    }

    [Fact]
    public void Resolve_NullReason_ReturnsFallbackPresentation()
    {
        // Act
        var presentation = HostedAuthErrorPresentationMapper.Resolve(null);

        // Assert
        Assert.Equal(
            HostedAuthErrorPresentationMapper.Resolve(HostedAuthErrorRoutes.SignInUnknown).Title,
            presentation.Title);
    }
}
