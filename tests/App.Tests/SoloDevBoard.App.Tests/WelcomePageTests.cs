using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Auth.Pages;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the hosted sign-in <see cref="Welcome"/> page.</summary>
public sealed class WelcomePageTests : BunitContext
{
    /// <summary>Initialises MudBlazor services for component rendering.</summary>
    public WelcomePageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddOptions();
        Services.AddSingleton<AuthenticationStateProvider>(new UnauthenticatedAuthenticationStateProvider());
    }

    [Fact]
    public void Welcome_WhenRendered_DisplaysSignInCallToAction()
    {
        // Arrange
        Services.AddSingleton(Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }));

        // Act
        var cut = Render<Welcome>();

        // Assert
        Assert.Contains("Sign in with GitHub", cut.Markup);
        var signInButton = cut.Find("[data-testid='welcome-sign-in']");
        Assert.Equal("/auth/sign-in", signInButton.GetAttribute("href"));
    }

    [Fact]
    public void Welcome_WhenReturnUrlPresent_ForwardsReturnUrlToSignInLink()
    {
        // Arrange
        Services.AddSingleton(Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/welcome?ReturnUrl=%2Fabout", forceLoad: true);

        // Act
        var cut = Render<Welcome>();

        // Assert
        var signInButton = cut.Find("[data-testid='welcome-sign-in']");
        Assert.Equal("/auth/sign-in?returnUrl=%2Fabout", signInButton.GetAttribute("href"));
    }

    [Fact]
    public void Welcome_WhenHostedSignInDisabled_RedirectsToHome()
    {
        // Arrange
        Services.AddSingleton(Options.Create(new GitHubAuthOptions { HostedSignInEnabled = false }));
        var navigationManager = Services.GetRequiredService<NavigationManager>();

        // Act
        Render<Welcome>();

        // Assert
        Assert.Equal("http://localhost/", navigationManager.Uri);
    }

    [Fact]
    public void Welcome_WhenAuthenticated_RedirectsToReturnUrl()
    {
        // Arrange
        Services.AddSingleton(Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }));
        Services.AddSingleton<AuthenticationStateProvider>(new AuthenticatedAuthenticationStateProvider());
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/welcome?returnUrl=%2Fabout", forceLoad: true);

        // Act
        Render<Welcome>();

        // Assert
        Assert.Equal("http://localhost/about", navigationManager.Uri);
    }

    private sealed class UnauthenticatedAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class AuthenticatedAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                    [
                        new System.Security.Claims.Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
                    ],
                    authenticationType: "test"))));
    }
}
