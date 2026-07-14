using Microsoft.AspNetCore.Http;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

public sealed class HostedAuthErrorPageRendererTests
{
    [Fact]
    public void Render_AccessDeniedReason_IncludesUserFacingCopy()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var html = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.AccessDenied,
            new GitHubAuthOptions());

        // Assert
        Assert.Contains("Access denied", html);
        Assert.Contains("not authorised for this deployment", html);
        Assert.Contains("data-testid=\"auth-error-try-again\"", html);
    }

    [Fact]
    public void Render_AccessDeniedWithAuthenticatedUser_ShowsSignedInLogin()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                [
                    new System.Security.Claims.Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
                ],
                authenticationType: "test")),
        };

        // Act
        var html = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.AccessDenied,
            new GitHubAuthOptions());

        // Assert
        Assert.Contains("Signed in as", html);
        Assert.Contains("markheydon", html);
        Assert.Contains("data-testid=\"auth-error-sign-out\"", html);
    }

    [Fact]
    public void Render_UnknownReason_UsesFallbackCopy()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var html = HostedAuthErrorPageRenderer.Render(context, "not-real", new GitHubAuthOptions());

        // Assert
        Assert.Contains("Sign-in failed", html);
        Assert.Contains("Hosted sign-in could not be completed", html);
    }
}
