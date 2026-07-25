using MudBlazor.Utilities;
using SoloDevBoard.App.Theming;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="ThemePreferenceConstants"/>.</summary>
public sealed class ThemePreferenceConstantsTests
{
    [Fact]
    public void FlashBackgrounds_MatchSoloDevBoardThemePaletteBackgrounds()
    {
        Assert.Equal(
            SoloDevBoardTheme.MudTheme.PaletteLight!.Background!.ToString(MudColorOutputFormats.Hex),
            ThemePreferenceConstants.LightBackground);
        Assert.Equal(
            SoloDevBoardTheme.MudTheme.PaletteDark!.Background!.ToString(MudColorOutputFormats.Hex),
            ThemePreferenceConstants.DarkBackground);
    }

    [Fact]
    public void JavaScriptConstants_MatchThemePreferenceConstants()
    {
        var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var repositoryRoot = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", "..", ".."));
        var constantsPath = Path.Combine(
            repositoryRoot,
            "src", "App", "SoloDevBoard.App", "wwwroot", "js", "themePreferenceConstants.js");
        var constantsSource = File.ReadAllText(constantsPath);

        Assert.Contains($"storageKey: '{ThemePreferenceConstants.StorageKey}'", constantsSource, StringComparison.Ordinal);
        Assert.Contains($"lightBackground: '{ThemePreferenceConstants.LightBackground}'", constantsSource, StringComparison.Ordinal);
        Assert.Contains($"darkBackground: '{ThemePreferenceConstants.DarkBackground}'", constantsSource, StringComparison.Ordinal);
    }
}
