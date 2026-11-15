using MudBlazor;
using MudBlazor.Utilities;
using SoloDevBoard.Themes;

namespace SoloDevBoard.Web.Tests;

/// <summary>CSS and asset fallbacks must match <see cref="SoloDevBoardTheme"/> light palette values.</summary>
public sealed class SoloDevBoardThemeFallbackTests
{
    private static string RepositoryRoot
    {
        get
        {
            var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            return Path.GetFullPath(Path.Combine(testProjectDirectory, "..", ".."));
        }
    }

    private static PaletteLight LightPalette =>
        (PaletteLight)SoloDevBoardTheme.MudTheme.PaletteLight!;

    private static string LightHex(Func<PaletteLight, MudColor> selector) =>
        selector(LightPalette).ToString(MudColorOutputFormats.Hex);

    [Fact]
    public void PaletteLight_DefinesTertiaryColours()
    {
        Assert.Equal("#8250df", LightHex(p => p.Tertiary));
        Assert.Equal("#ffffff", LightHex(p => p.TertiaryContrastText));
    }

    [Fact]
    public void PaletteDark_DefinesTertiaryColours()
    {
        var palette = SoloDevBoardTheme.MudTheme.PaletteDark!;
        Assert.Equal("#a371f7", palette.Tertiary!.ToString(MudColorOutputFormats.Hex));
        Assert.Equal("#0d1117", palette.TertiaryContrastText!.ToString(MudColorOutputFormats.Hex));
    }

    [Fact]
    public void ReconnectModalCss_FallbacksMatchLightPalette()
    {
        var css = ReadRepoFile("src", "SoloDevBoard.Web", "Components", "Shell", "Layout", "ReconnectModal.razor.css");
        Assert.Contains($"var(--mud-palette-primary, {LightHex(p => p.Primary)})", css, StringComparison.Ordinal);
        Assert.Contains($"var(--mud-palette-info, {LightHex(p => p.Info)})", css, StringComparison.Ordinal);
        Assert.Contains($"var(--mud-palette-warning, {LightHex(p => p.Warning)})", css, StringComparison.Ordinal);
        Assert.Contains($"var(--mud-palette-lines-default, {LightHex(p => p.LinesDefault)})", css, StringComparison.Ordinal);
        Assert.Contains($"var(--mud-palette-error, {LightHex(p => p.Error)})", css, StringComparison.Ordinal);
        Assert.Contains($"var(--mud-palette-text-secondary, {LightHex(p => p.TextSecondary)})", css, StringComparison.Ordinal);
        Assert.Contains(
            $"var(--mud-default-borderradius, {SoloDevBoardTheme.MudTheme.LayoutProperties.DefaultBorderRadius})",
            css,
            StringComparison.Ordinal);
        Assert.DoesNotContain("#594AE2", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainLayoutCss_SkipLinkFallbackMatchesPrimary()
    {
        var css = ReadRepoFile("src", "SoloDevBoard.Web", "Components", "Shell", "Layout", "MainLayout.razor.css");
        Assert.Contains($"var(--mud-palette-primary, {LightHex(p => p.Primary)})", css, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicSiteFocusFallback_MatchesLightPrimary()
    {
        var html = ReadRepoFile("website", "layouts", "_partials", "custom", "head-end.html");
        Assert.Contains($"var(--color-primary-500, {LightHex(p => p.Primary)})", html, StringComparison.Ordinal);
        Assert.DoesNotContain("#3b82f6", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrandMarkSvgs_UseLightPrimaryFill()
    {
        var primary = LightHex(p => p.Primary);
        foreach (var relativeParts in new[]
        {
            new[] { "src", "SoloDevBoard.Web", "wwwroot", "favicon.svg" },
            new[] { "docs", "assets", "github-app", "icon.svg" },
        })
        {
            var svg = ReadRepoFile(relativeParts);
            Assert.Contains($"fill=\"{primary}\"", svg, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadRepoFile(params string[] relativeParts) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, Path.Combine(relativeParts)));
}
