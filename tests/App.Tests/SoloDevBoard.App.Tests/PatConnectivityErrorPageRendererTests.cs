using Microsoft.AspNetCore.Http;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="PatConnectivityErrorPageRenderer"/>.</summary>
public sealed class PatConnectivityErrorPageRendererTests
{
    [Fact]
    public void Render_TokenRejectedReason_IncludesPatSpecificCopy()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var page = PatConnectivityErrorPageRenderer.Render(
            context,
            PatConnectivityErrorRoutes.TokenRejected);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, page.StatusCode);
        Assert.Contains("GitHub connection problem", page.Html);
        Assert.Contains("personal access token", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a hosted sign-in session problem", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-testid=\"pat-connectivity-return-home\"", page.Html);
    }
}
