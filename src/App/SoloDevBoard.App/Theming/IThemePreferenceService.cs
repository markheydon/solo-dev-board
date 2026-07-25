namespace SoloDevBoard.App.Theming;

/// <summary>Manages the user's theme appearance preference for the application shell.</summary>
public interface IThemePreferenceService
{
    /// <summary>Occurs when the stored preference changes.</summary>
    event Action? PreferenceChanged;

    /// <summary>Gets the current theme preference.</summary>
    ThemePreference Current { get; }

    /// <summary>Gets the resolved dark-mode state for manual light and dark preferences.</summary>
    bool EffectiveIsDarkMode { get; }

    /// <summary>Gets a value indicating whether the app should observe the system theme.</summary>
    bool ObserveSystemPreference { get; }

    /// <summary>Gets a value indicating whether the stored preference has been loaded.</summary>
    bool IsInitialised { get; }

    /// <summary>Loads the stored preference from browser storage.</summary>
    Task InitialiseAsync();

    /// <summary>Advances the preference through Automatic → Light → Dark.</summary>
    Task CycleAsync();
}
