using MudBlazor;

namespace SoloDevBoard.Themes;

/// <summary>
/// Defines the application's themes, including light and dark palettes, typography, and layout properties.
/// </summary>
public static class SoloDevBoardTheme
{
        // Light palette inspired by GitHub.
        private static PaletteLight PaletteLight => new PaletteLight
        {
            Primary = "#1b1f23",
            Secondary = "#2188ff",
            Surface = "#f6f8fa",
            Background = "#ffffff",
            TextPrimary = "#24292e",
            TextSecondary = "#57606a",
            ActionDefault = "#d1d5da",
            DrawerBackground = "#f6f8fa",
            AppbarBackground = "#ffffff",
            AppbarText = "#24292e",
            Success = "#238636",
            Warning = "#d29922",
            Error = "#cf222e",
            Info = "#0969da",
        };

        // Dark palette inspired by GitHub.
        private static PaletteDark PaletteDark => new PaletteDark
        {
            Primary = "#c9d1d9",
            Secondary = "#2188ff",
            Surface = "#161b22",
            Background = "#0d1117",
            TextPrimary = "#c9d1d9",
            TextSecondary = "#8b949e",
            ActionDefault = "#21262d",
            DrawerBackground = "#161b22",
            AppbarBackground = "#0d1117",
            AppbarText = "#c9d1d9",
            Success = "#238636",
            Warning = "#d29922",
            Error = "#cf222e",
            Info = "#0969da",            
        };

        /// <summary>
        /// The main theme for the SoloDevBoard application, combining the light and dark palettes with typography and layout properties.
        /// </summary>
        public static MudTheme MudTheme => new MudTheme
        {
            PaletteLight = PaletteLight,
            PaletteDark = PaletteDark,
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Helvetica Neue", "Arial", "sans-serif"]
                }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "6px",
                DrawerWidthLeft = "240px"
            }
        };
}