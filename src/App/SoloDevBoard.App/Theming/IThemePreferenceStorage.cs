namespace SoloDevBoard.App.Theming;

/// <summary>Reads and writes theme preference values from browser storage.</summary>
public interface IThemePreferenceStorage
{
    /// <summary>Gets the stored theme preference.</summary>
    /// <returns>The stored preference, or <see cref="ThemePreference.System"/> when unset.</returns>
    Task<ThemePreference> GetPreferenceAsync();

    /// <summary>Persists the theme preference.</summary>
    /// <param name="preference">The preference to store.</param>
    Task SetPreferenceAsync(ThemePreference preference);

    /// <summary>Gets whether the operating system currently prefers dark mode.</summary>
    /// <returns><see langword="true"/> when the system prefers dark mode; otherwise, <see langword="false"/>.</returns>
    Task<bool> GetSystemIsDarkModeAsync();
}
