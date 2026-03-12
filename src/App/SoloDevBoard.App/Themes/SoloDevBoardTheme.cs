using MudBlazor;

namespace SoloDevBoard.Themes;

/// <summary>
/// Defines the application's themes, including light and dark palettes, typography, and layout properties.
/// </summary>
public static class SoloDevBoardTheme
{
    // Light palette inspired by GitHub.
    private static PaletteLight PaletteLight => new()
    {
        Primary = "#2da44e",
        Secondary = "#2188ff",
        Surface = "#f6f8fa",
        Background = "#ffffff",
        TextPrimary = "#24292e",
        TextSecondary = "#57606a",
        TextDisabled = "#8c959f",
        ActionDefault = "#57606a",
        ActionDisabled = "#8c959f99",
        ActionDisabledBackground = "#afb8c133",
        DrawerBackground = "#f6f8fa",
        AppbarBackground = "#ffffff",
        AppbarText = "#24292e",
        Success = "#238636",
        Warning = "#d29922",
        Error = "#cf222e",
        Info = "#0969da",
        LinesDefault = "#d0d7de",
        TableLines = "#d0d7de",
        Divider = "#d8dee4",
        OverlayLight = "#00000080",
    };

    // Dark palette inspired by GitHub.
    private static PaletteDark PaletteDark => new()
    {
        Primary = "#2ea043",
        Secondary = "#2f81f7",
        Surface = "#161b22",
        Background = "#0d1117",
        TextPrimary = "#c9d1d9",
        TextSecondary = "#8b949e",
        TextDisabled = "#6e7681",
        ActionDefault = "#8b949e",
        ActionDisabled = "#6e7681b3",
        ActionDisabledBackground = "#30363d80",
        DrawerBackground = "#161b22",
        AppbarBackground = "#0d1117",
        AppbarText = "#c9d1d9",
        Success = "#238636",
        Warning = "#d29922",
        Error = "#cf222e",
        Info = "#1f6feb",
        LinesDefault = "#30363d",
        TableLines = "#30363d",
        Divider = "#30363d",
        OverlayLight = "#00000099",
    };

    /// <summary>
    /// The main theme for the SoloDevBoard application, combining the light and dark palettes with typography and layout properties.
    /// </summary>
    public static MudTheme MudTheme => new()
    {
        PaletteLight = PaletteLight,
        PaletteDark = PaletteDark,
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Helvetica Neue", "Arial", "sans-serif"],
            },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "240px",
        },
    };
}