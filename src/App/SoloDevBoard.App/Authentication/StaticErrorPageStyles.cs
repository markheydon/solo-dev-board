using System.Text;
using MudBlazor.Utilities;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Authentication;

/// <summary>Shared CSS for static HTML error pages served outside the Blazor shell.</summary>
internal static class StaticErrorPageStyles
{
    /// <summary>Appends a style block that mirrors the light <see cref="SoloDevBoardTheme"/> palette.</summary>
    /// <param name="builder">The HTML builder to append to.</param>
    public static void AppendStyleBlock(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var theme = SoloDevBoardTheme.MudTheme;
        var palette = theme.PaletteLight;
        var borderRadius = theme.LayoutProperties.DefaultBorderRadius;
        var fontFamily = FormatFontFamily(theme.Typography.Default.FontFamily ?? []);

        builder.AppendLine("  <style>");
        builder.AppendLine($"    body {{ font-family: {fontFamily}; margin: 0; background: {ToCssColor(palette.Background)}; color: {ToCssColor(palette.TextPrimary)}; }}");
        builder.AppendLine($"  .app-bar {{ background: {ToCssColor(palette.AppbarBackground)}; color: {ToCssColor(palette.AppbarText)}; padding: 1rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,.12); border-bottom: 1px solid {ToCssColor(palette.LinesDefault)}; }}");
        builder.AppendLine("  .app-bar h1 { margin: 0; font-size: 1.5rem; font-weight: 500; }");
        builder.AppendLine("  main { max-width: 36rem; margin: 0 auto; padding: 2rem 1.5rem; }");
        builder.AppendLine($"  .card {{ background: {ToCssColor(palette.Background)}; border-radius: {borderRadius}; border: 1px solid {ToCssColor(palette.LinesDefault)}; padding: 1rem; margin: 1rem 0; }}");
        builder.AppendLine("  .actions { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 1.5rem; }");
        builder.AppendLine($"  .button {{ display: inline-block; border: none; border-radius: {borderRadius}; padding: 0.6rem 1rem; font: inherit; text-decoration: none; cursor: pointer; }}");
        builder.AppendLine($"  .button-primary {{ background: {ToCssColor(palette.Primary)}; color: {ToCssColor(palette.PrimaryContrastText)}; }}");
        builder.AppendLine($"  .button-secondary {{ background: transparent; color: {ToCssColor(palette.Primary)}; border: 1px solid {ToCssColor(palette.Primary)}; }}");
        builder.AppendLine("  </style>");
    }

    private static string FormatFontFamily(string[] fontFamily)
    {
        return string.Join(", ", fontFamily.Select(font => font.Contains(' ', StringComparison.Ordinal) ? $"\"{font}\"" : font));
    }

    private static string ToCssColor(MudColor color) => color.ToString(MudColorOutputFormats.Hex);
}
