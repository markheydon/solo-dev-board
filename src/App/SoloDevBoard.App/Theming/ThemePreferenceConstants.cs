using MudBlazor.Utilities;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Theming;

/// <summary>Shared constants for theme preference persistence and load-time flash styling.</summary>
public static class ThemePreferenceConstants
{
    /// <summary>Browser <c>localStorage</c> key for the theme preference.</summary>
    public const string StorageKey = "solo-dev-board.theme-preference";

    /// <summary>Light-mode page background used before MudBlazor initialises.</summary>
    public static string LightBackground => SoloDevBoardTheme.MudTheme.PaletteLight!.Background!.ToString(MudColorOutputFormats.Hex);

    /// <summary>Dark-mode page background used before MudBlazor initialises.</summary>
    public static string DarkBackground => SoloDevBoardTheme.MudTheme.PaletteDark!.Background!.ToString(MudColorOutputFormats.Hex);
}
