using MudBlazor;

namespace SoloDevBoard.Themes;

/// <summary>
/// Defines the application's themes, including light and dark palettes, typography, and layout properties.
/// </summary>
public static class SoloDevBoardTheme
{
    // Light palette inspired by GitHub, tuned for WCAG 2.1 AA contrast (issue #253).
    private static PaletteLight PaletteLight => new()
    {
        Primary = "#167c38",
        PrimaryContrastText = "#ffffff",
        Secondary = "#0969da",
        SecondaryContrastText = "#ffffff",
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
        Success = "#167c38",
        SuccessContrastText = "#ffffff",
        Warning = "#9a6700",
        WarningContrastText = "#ffffff",
        Error = "#cf222e",
        ErrorContrastText = "#ffffff",
        Info = "#0969da",
        InfoContrastText = "#ffffff",
        LinesDefault = "#d0d7de",
        TableLines = "#d0d7de",
        Divider = "#d8dee4",
        OverlayLight = "#00000080",
    };

    // Dark palette inspired by GitHub, tuned for WCAG 2.1 AA contrast (issue #253).
    private static PaletteDark PaletteDark => new()
    {
        Primary = "#3fb950",
        PrimaryContrastText = "#0d1117",
        Secondary = "#58a6ff",
        SecondaryContrastText = "#0d1117",
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
        Success = "#3fb950",
        SuccessContrastText = "#0d1117",
        Warning = "#d29922",
        WarningContrastText = "#0d1117",
        Error = "#ff7b72",
        ErrorContrastText = "#0d1117",
        Info = "#58a6ff",
        InfoContrastText = "#0d1117",
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
