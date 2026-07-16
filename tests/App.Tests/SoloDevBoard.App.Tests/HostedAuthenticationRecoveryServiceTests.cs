using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.App.Tests;

public sealed class HostedAuthenticationRecoveryServiceTests
{
    [Fact]
    public void TryInitiateRecovery_HostedAuthenticationRequiredException_NavigatesToSessionExpiredRoute()
    {
        // Arrange
        var navigationManager = new TestNavigationManager("https://localhost/", "https://localhost/repositories");
        var service = new HostedAuthenticationRecoveryService(
            navigationManager,
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }));

        // Act
        var result = service.TryInitiateRecovery(new HostedAuthenticationRequiredException());

        // Assert
        Assert.True(result);
        Assert.Contains("/auth/session-expired", navigationManager.LastUri, StringComparison.Ordinal);
        Assert.Contains("returnUrl=%2Frepositories", navigationManager.LastUri, StringComparison.Ordinal);
        Assert.True(navigationManager.ForceLoad);
    }

    [Fact]
    public void TryInitiateRecovery_HostedSignInDisabled_ReturnsFalse()
    {
        // Arrange
        var navigationManager = new TestNavigationManager("https://localhost/", "https://localhost/");
        var service = new HostedAuthenticationRecoveryService(
            navigationManager,
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = false }));

        // Act
        var result = service.TryInitiateRecovery(new HostedAuthenticationRequiredException());

        // Assert
        Assert.False(result);
        Assert.Null(navigationManager.LastUri);
    }

    [Fact]
    public void TryInitiateRecovery_UnrelatedException_ReturnsFalse()
    {
        // Arrange
        var navigationManager = new TestNavigationManager("https://localhost/", "https://localhost/");
        var service = new HostedAuthenticationRecoveryService(
            navigationManager,
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }));

        // Act
        var result = service.TryInitiateRecovery(new InvalidOperationException("Unrelated."));

        // Assert
        Assert.False(result);
        Assert.Null(navigationManager.LastUri);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        public string? LastUri { get; private set; }

        public bool ForceLoad { get; private set; }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            LastUri = uri;
            ForceLoad = options.ForceLoad;
        }
    }
}
