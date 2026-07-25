namespace SoloDevBoard.App.Theming;

/// <summary>Coordinates theme preference state between the shell and browser storage.</summary>
public sealed class ThemePreferenceService(IThemePreferenceStorage storage) : IThemePreferenceService
{
    private bool initialised;

    /// <inheritdoc/>
    public event Action? PreferenceChanged;

    /// <inheritdoc/>
    public ThemePreference Current { get; private set; } = ThemePreference.System;

    /// <inheritdoc/>
    public bool EffectiveIsDarkMode { get; private set; }

    /// <inheritdoc/>
    public bool ObserveSystemPreference => Current == ThemePreference.System;

    /// <inheritdoc/>
    public bool IsInitialised => initialised;

    /// <inheritdoc/>
    public async Task InitialiseAsync()
    {
        if (initialised)
        {
            return;
        }

        Current = await storage.GetPreferenceAsync().ConfigureAwait(false);
        await RefreshEffectiveIsDarkModeAsync().ConfigureAwait(false);
        initialised = true;
        PreferenceChanged?.Invoke();
    }

    /// <inheritdoc/>
    public async Task CycleAsync()
    {
        if (!initialised)
        {
            await InitialiseAsync().ConfigureAwait(false);
        }

        Current = Current.GetNext();
        await storage.SetPreferenceAsync(Current).ConfigureAwait(false);
        await RefreshEffectiveIsDarkModeAsync().ConfigureAwait(false);
        PreferenceChanged?.Invoke();
    }

    private async Task RefreshEffectiveIsDarkModeAsync()
    {
        EffectiveIsDarkMode = Current switch
        {
            ThemePreference.Light => false,
            ThemePreference.Dark => true,
            ThemePreference.System => await storage.GetSystemIsDarkModeAsync().ConfigureAwait(false),
            _ => false,
        };
    }
}
