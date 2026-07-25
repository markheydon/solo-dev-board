using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Shell;
using SoloDevBoard.App.Theming;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for <see cref="AppThemeProvider"/>.</summary>
public sealed class AppThemeProviderTests : BunitContext
{
    /// <summary>Initialises MudBlazor services for component rendering.</summary>
    public AppThemeProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public async Task AppThemeProvider_WhenSystemPreferenceObserved_RendersSystemAwareMudThemeProvider()
    {
        var themePreferenceService = Substitute.For<IThemePreferenceService>();
        themePreferenceService.ObserveSystemPreference.Returns(true);
        themePreferenceService.InitialiseAsync().Returns(Task.CompletedTask);
        Services.AddSingleton(themePreferenceService);

        var cut = Render<AppThemeProvider>(parameters =>
            parameters.Add(p => p.Theme, SoloDevBoardTheme.MudTheme));

        await cut.InvokeAsync(() => Task.CompletedTask);

        var provider = cut.FindComponent<MudThemeProvider>();

#pragma warning disable MUD0012
        Assert.True(provider.Instance.ObserveSystemDarkModeChange);
#pragma warning restore MUD0012
    }

    [Fact]
    public async Task AppThemeProvider_WhenDarkPreferenceStored_RendersExplicitDarkModeProvider()
    {
        var storage = new FakeThemePreferenceStorage
        {
            StoredPreference = ThemePreference.Dark,
        };
        Services.AddSingleton<IThemePreferenceService>(new ThemePreferenceService(storage));

        var cut = Render<AppThemeProvider>(parameters =>
            parameters.Add(p => p.Theme, SoloDevBoardTheme.MudTheme));

        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.WaitForAssertion(() =>
        {
            var provider = cut.FindComponent<MudThemeProvider>();
#pragma warning disable MUD0012
            Assert.False(provider.Instance.ObserveSystemDarkModeChange);
            Assert.True(provider.Instance.IsDarkMode);
#pragma warning restore MUD0012
        });
    }

    [Fact]
    public async Task AppThemeProvider_WhenInitialised_SubscribesBeforePreferenceChangedIsRaised()
    {
        var storage = new FakeThemePreferenceStorage
        {
            StoredPreference = ThemePreference.Light,
        };
        Services.AddSingleton<IThemePreferenceService>(new ThemePreferenceService(storage));

        var cut = Render<AppThemeProvider>(parameters =>
            parameters.Add(p => p.Theme, SoloDevBoardTheme.MudTheme));

        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.WaitForAssertion(() =>
        {
            var provider = cut.FindComponent<MudThemeProvider>();
#pragma warning disable MUD0012
            Assert.False(provider.Instance.ObserveSystemDarkModeChange);
            Assert.False(provider.Instance.IsDarkMode);
#pragma warning restore MUD0012
        });
    }

    private sealed class FakeThemePreferenceStorage : IThemePreferenceStorage
    {
        public ThemePreference StoredPreference { get; set; } = ThemePreference.System;

        public bool SystemIsDarkMode { get; set; }

        public Task<ThemePreference> GetPreferenceAsync() => Task.FromResult(StoredPreference);

        public Task SetPreferenceAsync(ThemePreference preference)
        {
            StoredPreference = preference;
            return Task.CompletedTask;
        }

        public Task<bool> GetSystemIsDarkModeAsync() => Task.FromResult(SystemIsDarkMode);
    }
}
