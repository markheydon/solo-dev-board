using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Shell.Layout;
using SoloDevBoard.App.Theming;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for shell theme controls in <see cref="MainLayout"/>.</summary>
public sealed class MainLayoutThemeTests : BunitContext
{
    private readonly IThemePreferenceService _themePreferenceService = Substitute.For<IThemePreferenceService>();
    private readonly IGitHubConnectivityStatusService _connectivityStatusService = Substitute.For<IGitHubConnectivityStatusService>();

    /// <summary>Initialises MudBlazor services and theme preference test doubles.</summary>
    public MainLayoutThemeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddOptions();
        Services.AddSingleton<AuthenticationStateProvider>(new UnauthenticatedAuthenticationStateProvider());
        Services.AddSingleton(Options.Create(new GitHubAuthOptions { HostedSignInEnabled = false }));
        Services.AddSingleton(_connectivityStatusService);
        Services.AddSingleton(_themePreferenceService);

        _connectivityStatusService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new GitHubConnectivityStatusDto(true, "markheydon", "Connected as @markheydon."));
        _themePreferenceService.Current.Returns(ThemePreference.System);
        _themePreferenceService.ObserveSystemPreference.Returns(true);
        _themePreferenceService.IsInitialised.Returns(true);
        _themePreferenceService.InitialiseAsync().Returns(Task.CompletedTask);
        _themePreferenceService.CycleAsync().Returns(Task.CompletedTask);
    }

    [Theory]
    [InlineData(ThemePreference.System, "Theme: automatic (follow system). Activate light mode.")]
    [InlineData(ThemePreference.Light, "Theme: light. Activate dark mode.")]
    [InlineData(ThemePreference.Dark, "Theme: dark. Activate automatic mode.")]
    public void MainLayout_WhenRendered_ExposesThemeButtonAriaLabelForCurrentPreference(
        ThemePreference preference,
        string expectedAriaLabel)
    {
        _themePreferenceService.Current.Returns(preference);
        _themePreferenceService.ObserveSystemPreference.Returns(preference == ThemePreference.System);
        _themePreferenceService.IsInitialised.Returns(true);

        var cut = Render<MainLayout>();

        var themeButton = cut.Find("button[aria-label^='Theme:']");

        Assert.Equal(expectedAriaLabel, themeButton.GetAttribute("aria-label"));
    }

    [Fact]
    public void MainLayout_WhenThemePreferenceNotInitialised_HidesThemeButton()
    {
        _themePreferenceService.IsInitialised.Returns(false);

        var cut = Render<MainLayout>();

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("button[aria-label^='Theme:']"));
    }

    [Fact]
    public async Task MainLayout_WhenThemeButtonClicked_CyclesThemePreference()
    {
        var cut = Render<MainLayout>();

        await cut.Find("button[aria-label='Theme: automatic (follow system). Activate light mode.']").ClickAsync();

        await _themePreferenceService.Received(1).CycleAsync();
    }

    private sealed class UnauthenticatedAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
