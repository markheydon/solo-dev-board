using SoloDevBoard.App.Theming;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="ThemePreferenceService"/>.</summary>
public sealed class ThemePreferenceServiceTests
{
    [Fact]
    public async Task InitialiseAsync_WhenNoStoredPreference_DefaultsToSystem()
    {
        var storage = new FakeThemePreferenceStorage();
        var service = new ThemePreferenceService(storage);

        await service.InitialiseAsync();

        Assert.Equal(ThemePreference.System, service.Current);
        Assert.True(service.ObserveSystemPreference);
        Assert.False(service.EffectiveIsDarkMode);
        Assert.True(service.IsInitialised);
    }

    [Fact]
    public async Task InitialiseAsync_WhenDarkPreferenceStored_ResolvesEffectiveDarkMode()
    {
        var storage = new FakeThemePreferenceStorage
        {
            StoredPreference = ThemePreference.Dark,
        };
        var service = new ThemePreferenceService(storage);

        await service.InitialiseAsync();

        Assert.Equal(ThemePreference.Dark, service.Current);
        Assert.False(service.ObserveSystemPreference);
        Assert.True(service.EffectiveIsDarkMode);
    }

    [Fact]
    public async Task CycleAsync_FromSystem_AdvancesToLightThenDarkThenSystem()
    {
        var storage = new FakeThemePreferenceStorage
        {
            SystemIsDarkMode = true,
        };
        var service = new ThemePreferenceService(storage);
        await service.InitialiseAsync();

        await service.CycleAsync();
        Assert.Equal(ThemePreference.Light, service.Current);
        Assert.False(service.EffectiveIsDarkMode);
        Assert.Equal(ThemePreference.Light, storage.StoredPreference);

        await service.CycleAsync();
        Assert.Equal(ThemePreference.Dark, service.Current);
        Assert.True(service.EffectiveIsDarkMode);
        Assert.Equal(ThemePreference.Dark, storage.StoredPreference);

        await service.CycleAsync();
        Assert.Equal(ThemePreference.System, service.Current);
        Assert.True(service.ObserveSystemPreference);
        Assert.True(service.EffectiveIsDarkMode);
        Assert.Equal(ThemePreference.System, storage.StoredPreference);
    }

    [Fact]
    public async Task CycleAsync_RaisesPreferenceChanged()
    {
        var storage = new FakeThemePreferenceStorage();
        var service = new ThemePreferenceService(storage);
        await service.InitialiseAsync();

        var changeCount = 0;
        service.PreferenceChanged += () => changeCount++;

        await service.CycleAsync();

        Assert.Equal(1, changeCount);
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
