using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// WCAG 2.1 AA accessibility tests using axe-core.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AccessibilityTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Routes audited for accessibility in light mode.
    /// </summary>
    public static TheoryData<AccessibilityRoute> LightModeRoutes =>
        new(AccessibilityHelpers.Routes.ToArray());

    /// <summary>
    /// Routes audited for accessibility in dark mode.
    /// </summary>
    public static TheoryData<AccessibilityRoute> DarkModeRoutes =>
        new(AccessibilityHelpers.Routes.ToArray());

    /// <summary>
    /// Verifies each route has no critical or serious axe violations in light mode.
    /// </summary>
    [Theory]
    [MemberData(nameof(LightModeRoutes))]
    public async Task Route_HasNoCriticalOrSeriousViolations_Light(AccessibilityRoute route)
    {
        await AccessibilityHelpers.SeedThemePreferenceAsync(Page, "light");

        var response = await Page.GotoAsync(route.Path);
        Assert.True(response?.Ok == true || response?.Status == 401);

        await AccessibilityHelpers.WaitForAccessibilityScanReadyAsync(Page, route.Path, "light");
        await AccessibilityHelpers.ExpectNoCriticalOrSeriousViolationsAsync(Page, $"{route.Name} (light)");
    }

    /// <summary>
    /// Verifies each route has no critical or serious axe violations in dark mode.
    /// </summary>
    [Theory]
    [MemberData(nameof(DarkModeRoutes))]
    public async Task Route_HasNoCriticalOrSeriousViolations_Dark(AccessibilityRoute route)
    {
        await AccessibilityHelpers.SeedThemePreferenceAsync(Page, "dark");

        var response = await Page.GotoAsync(route.Path);
        Assert.True(response?.Ok == true || response?.Status == 401);

        await AccessibilityHelpers.WaitForAccessibilityScanReadyAsync(Page, route.Path, "dark");
        await AccessibilityHelpers.ExpectNoCriticalOrSeriousViolationsAsync(Page, $"{route.Name} (dark)");
    }

    /// <summary>
    /// Verifies the home shell exposes labelled navigation controls.
    /// </summary>
    [Fact]
    public async Task HomeShell_ExposesLabelledNavigationControls()
    {
        await AccessibilityHelpers.SeedThemePreferenceAsync(Page, "light");
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Skip to main content" })).ToBeAttachedAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation drawer" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Theme: light. Activate dark mode." })).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "More options" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
        await Expect(Page.Locator("#main-content")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies automatic mode follows the system colour scheme preference.
    /// </summary>
    [Fact]
    public async Task AutomaticMode_FollowsSystemColourSchemePreference()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.AddInitScriptAsync(
            $"localStorage.setItem('{ThemePreference.StorageKey}', 'system');");
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Theme: automatic (follow system). Activate light mode." })).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.Locator("html")).ToHaveCSSAsync("color-scheme", "dark");
    }

    /// <summary>
    /// Verifies the info snackbar meets WCAG 2.1 AA contrast requirements.
    /// </summary>
    [Fact]
    public async Task InfoSnackbar_MeetsWcag21AaContrastRequirements()
    {
        await Page.GotoAsync("/repositories");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Repositories", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
        await Expect(Page.Locator(".mud-snackbar").First).ToBeVisibleAsync();

        await AccessibilityHelpers.ExpectNoCriticalOrSeriousViolationsOnSelectorAsync(
            Page,
            ".mud-snackbar",
            "Repositories placeholder snackbar");
    }
}
