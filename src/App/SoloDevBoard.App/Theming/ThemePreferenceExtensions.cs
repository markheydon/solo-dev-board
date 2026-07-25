using MudBlazor;

namespace SoloDevBoard.App.Theming;

/// <summary>UI helpers for <see cref="ThemePreference"/> values.</summary>
public static class ThemePreferenceExtensions
{
    /// <summary>Returns the next preference when cycling the shell theme button.</summary>
    /// <param name="preference">The current preference.</param>
    /// <returns>The next preference in the Automatic → Light → Dark cycle.</returns>
    public static ThemePreference GetNext(this ThemePreference preference) => preference switch
    {
        ThemePreference.System => ThemePreference.Light,
        ThemePreference.Light => ThemePreference.Dark,
        ThemePreference.Dark => ThemePreference.System,
        _ => ThemePreference.System,
    };

    /// <summary>Gets the MudBlazor icon name for the theme button.</summary>
    /// <param name="preference">The current preference.</param>
    /// <returns>The Material icon identifier.</returns>
    public static string GetButtonIcon(this ThemePreference preference) => preference switch
    {
        ThemePreference.System => Icons.Material.Rounded.AutoMode,
        ThemePreference.Light => Icons.Material.Outlined.LightMode,
        ThemePreference.Dark => Icons.Material.Outlined.DarkMode,
        _ => Icons.Material.Rounded.AutoMode,
    };

    /// <summary>Gets the accessible name for the theme button.</summary>
    /// <param name="preference">The current preference.</param>
    /// <returns>The aria-label describing the current mode and next action.</returns>
    public static string GetButtonAriaLabel(this ThemePreference preference) => preference switch
    {
        ThemePreference.System => "Theme: automatic (follow system). Activate light mode.",
        ThemePreference.Light => "Theme: light. Activate dark mode.",
        ThemePreference.Dark => "Theme: dark. Activate automatic mode.",
        _ => "Theme: automatic (follow system). Activate light mode.",
    };

    /// <summary>Parses a persisted preference value.</summary>
    /// <param name="value">The stored preference string.</param>
    /// <returns>The parsed preference, or <see cref="ThemePreference.System"/> when unknown.</returns>
    public static ThemePreference ParsePreference(string? value) => value?.ToLowerInvariant() switch
    {
        "light" => ThemePreference.Light,
        "dark" => ThemePreference.Dark,
        "system" => ThemePreference.System,
        _ => ThemePreference.System,
    };

    /// <summary>Serialises a preference for browser storage.</summary>
    /// <param name="preference">The preference to store.</param>
    /// <returns>The lowercase storage value.</returns>
    public static string ToStorageValue(this ThemePreference preference) => preference switch
    {
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        ThemePreference.System => "system",
        _ => "system",
    };
}
