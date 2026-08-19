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
        var page = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.AccessDenied,
            new GitHubAuthOptions());

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, page.StatusCode);
        Assert.Contains("Access denied", page.Html);
        Assert.Contains("not authorised for this deployment", page.Html);
        Assert.Contains("data-testid=\"auth-error-try-again\"", page.Html);
        Assert.Contains("href=\"/favicon.svg\"", page.Html);
    }

    [Fact]
    public void Render_AccessDeniedReason_UsesApplicationThemeColours()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var page = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.AccessDenied,
            new GitHubAuthOptions());

        // Assert
        Assert.Contains("#167c38", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#ffffff", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#594ae2", page.Html, StringComparison.OrdinalIgnoreCase);
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
        var page = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.AccessDenied,
            new GitHubAuthOptions());

        // Assert
        Assert.Contains("Signed in as", page.Html);
        Assert.Contains("markheydon", page.Html);
        Assert.Contains("data-testid=\"auth-error-sign-out\"", page.Html);
    }

    [Fact]
    public void Render_SessionExpiredReason_IncludesSignInAgainLinkWithReturnUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?returnUrl=%2Frepositories");

        // Act
        var page = HostedAuthErrorPageRenderer.Render(
            context,
            HostedAuthErrorRoutes.SessionExpired,
            new GitHubAuthOptions());

        // Assert
        Assert.Contains("Session expired", page.Html);
        Assert.Contains("no longer valid", page.Html);
        Assert.Contains("Sign in again", page.Html);
        Assert.Contains("/auth/sign-in?returnUrl=%2Frepositories", page.Html);
    }

    [Fact]
    public void Render_UnknownReason_UsesFallbackCopy()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var page = HostedAuthErrorPageRenderer.Render(context, "not-real", new GitHubAuthOptions());

        // Assert
        Assert.Contains("Sign-in failed", page.Html);
        Assert.Contains("Hosted sign-in could not be completed", page.Html);
    }
}
