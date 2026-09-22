using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Primary feature routes exposed in the navigation drawer.
/// </summary>
public sealed record FeatureRoute(string NavLabel, string Path, string TitlePattern);

/// <summary>
/// Navigation helpers for drawer and feature routes.
/// </summary>
public static class NavigationHelpers
{
    /// <summary>
    /// Drawer routes used by navigation tests.
    /// </summary>
    public static IReadOnlyList<FeatureRoute> FeatureRoutes =>
    [
        new("Audit Dashboard", "/audit-dashboard", "Audit Dashboard"),
        new("Repositories", "/repositories", "Repositories"),
        new("Migrate", "/migrate", "One-Click Migration"),
        new("Labels", "/labels", "Label Manager"),
        new("Board Rules", "/board-rules", "Board Rules Visualiser"),
        new("Triage", "/triage", "Triage UI"),
        new("Actions Templates", "/actions-templates", "Actions Templates"),
        new("Planning", "/planning/daily-focus", "Planning — Daily Focus"),
    ];

    /// <summary>
    /// Navigates to a feature using the shell drawer link label.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="label">Drawer link label.</param>
    public static async Task NavigateViaDrawerAsync(IPage page, string label)
    {
        await EnsureDrawerOpenAsync(page);

        var link = page.Locator("#nav-drawer").GetByRole(AriaRole.Link, new() { Name = label, Exact = true });
        await Expect(link).ToBeVisibleAsync();

        // MudBlazor drawer transforms can leave links outside Playwright's clickable viewport.
        await link.EvaluateAsync("element => element.click()");
    }

    private static async Task EnsureDrawerOpenAsync(IPage page)
    {
        var drawer = page.Locator("#nav-drawer");
        var firstNavLink = drawer.GetByRole(AriaRole.Link).First;

        if (!await firstNavLink.IsVisibleAsync())
        {
            await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button).First.ClickAsync();
            await Expect(firstNavLink).ToBeVisibleAsync();
        }
    }
}
