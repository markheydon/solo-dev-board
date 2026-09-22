using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Appearance theme control tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AppearanceTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the theme button cycles automatic, light, and dark modes.
    /// </summary>
    [Fact]
    public async Task ThemeButton_CyclesAutomaticLightAndDarkModes()
    {
        await Page.GotoAsync("/");

        var themeButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Theme:") });
        await Expect(themeButton).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Expect(themeButton).ToHaveAccessibleNameAsync(
            new Regex(@"Theme: automatic \(follow system\)\. Activate light mode\.", RegexOptions.IgnoreCase));

        await themeButton.ClickAsync();
        await Expect(themeButton).ToHaveAccessibleNameAsync(
            new Regex(@"Theme: light\. Activate dark mode\.", RegexOptions.IgnoreCase));

        await themeButton.ClickAsync();
        await Expect(themeButton).ToHaveAccessibleNameAsync(
            new Regex(@"Theme: dark\. Activate automatic mode\.", RegexOptions.IgnoreCase));

        await themeButton.ClickAsync();
        await Expect(themeButton).ToHaveAccessibleNameAsync(
            new Regex(@"Theme: automatic \(follow system\)\. Activate light mode\.", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Verifies the selected theme mode persists in browser storage.
    /// </summary>
    [Fact]
    public async Task SelectedThemeMode_PersistsInBrowserStorage()
    {
        await Page.GotoAsync("/");

        var themeButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Theme:") });
        await Expect(themeButton).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await themeButton.ClickAsync();

        var deadline = DateTime.UtcNow.AddSeconds(15);
        string? storedValue = null;

        while (DateTime.UtcNow < deadline)
        {
            storedValue = await Page.EvaluateAsync<string?>(
                "(key) => localStorage.getItem(key)",
                ThemePreference.StorageKey);

            if (storedValue == "light")
            {
                break;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Equal("light", storedValue);

        await Page.ReloadAsync();
        await Expect(themeButton).ToHaveAccessibleNameAsync(
            new Regex(@"Theme: light\. Activate dark mode\.", RegexOptions.IgnoreCase));
    }
}
