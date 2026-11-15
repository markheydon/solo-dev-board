using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Accessibility route metadata for axe scans.
/// </summary>
public sealed record AccessibilityRoute(string Path, string Name);

/// <summary>
/// axe-core accessibility helpers for WCAG 2.1 AA scans.
/// </summary>
public static class AccessibilityHelpers
{
    private static readonly List<string> WcagTags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];

    private static readonly IReadOnlyDictionary<string, string[]> RouteLoadingTestIds =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["/repositories"] = ["repositories-loading-state"],
            ["/audit-dashboard"] = ["audit-loading-state"],
            ["/labels"] = ["labels-loading-state"],
            ["/board-rules"] = ["board-rules-repositories-loading-state"],
            ["/triage"] = ["triage-loading-repositories"],
            ["/actions-templates"] = ["actions-templates-repositories-loading-state", "actions-templates-loading-state"],
        };

    private static readonly IReadOnlyDictionary<string, string> ResolvedThemeButtonLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["light"] = "Theme: light. Activate dark mode.",
            ["dark"] = "Theme: dark. Activate automatic mode.",
        };

    /// <summary>
    /// Routes audited for WCAG 2.1 AA in the CI accessibility suite.
    /// </summary>
    public static IReadOnlyList<AccessibilityRoute> Routes =>
    [
        new("/", "Home"),
        new("/about", "About"),
        new("/auth/connectivity-error?reason=token-rejected", "PAT connectivity error"),
        new("/audit-dashboard", "Audit Dashboard"),
        new("/repositories", "Repositories"),
        new("/migrate", "One-Click Migration"),
        new("/labels", "Label Manager"),
        new("/board-rules", "Board Rules"),
        new("/triage", "Triage"),
        new("/actions-templates", "Actions Templates"),
        new("/planning/daily-focus", "Planning — Daily Focus"),
        new("/planning/backlog", "Planning Backlog Review"),
        new("/planning/iteration", "Planning Iteration"),
    ];

    /// <summary>
    /// Runs axe-core against the current page and asserts no critical or serious violations.
    /// </summary>
    public static async Task ExpectNoCriticalOrSeriousViolationsAsync(IPage page, string context)
    {
        var runContext = new AxeRunContext
        {
            Exclude = [new AxeSelector(".mud-snackbar")],
        };

        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = WcagTags },
        };

        var results = await page.RunAxe(runContext, options);
        AssertNoBlockingViolations(results.Violations, context);
    }

    /// <summary>
    /// Runs axe-core against a selector on the current page.
    /// </summary>
    public static async Task ExpectNoCriticalOrSeriousViolationsOnSelectorAsync(IPage page, string selector, string context)
    {
        var runContext = new AxeRunContext
        {
            Include = [new AxeSelector(selector)],
        };

        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = WcagTags },
        };

        var results = await page.RunAxe(runContext, options);
        AssertNoBlockingViolations(results.Violations, context);
    }

    /// <summary>
    /// Waits until the shell theme preference has been applied to the document.
    /// </summary>
    public static async Task WaitForResolvedThemeAsync(IPage page, string theme)
    {
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = ResolvedThemeButtonLabels[theme] }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(page.Locator("html")).ToHaveCSSAsync("color-scheme", theme);
    }

    /// <summary>
    /// Waits for the page to finish rendering before accessibility scans.
    /// </summary>
    public static async Task WaitForAccessibilityScanReadyAsync(IPage page, string path, string theme)
    {
        if (path.StartsWith("/auth/connectivity-error", StringComparison.Ordinal))
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "GitHub connection problem" })).ToBeVisibleAsync();
            return;
        }

        await Expect(page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
        await WaitForResolvedThemeAsync(page, theme);

        var routePath = path.Split('?')[0];
        if (RouteLoadingTestIds.TryGetValue(routePath, out var loadingTestIds))
        {
            foreach (var testId in loadingTestIds)
            {
                await Expect(page.GetByTestId(testId)).ToBeHiddenAsync(new() { Timeout = 15_000 });
            }
        }

        if (routePath == "/repositories")
        {
            await Expect(page.GetByTestId("repositories-reload-from-github-button")).ToBeEnabledAsync(new() { Timeout = 15_000 });
        }

        if (routePath is "/planning/daily-focus" or "/planning/backlog" or "/planning/iteration" or "/planning/repos")
        {
            await Expect(page.GetByTestId("planning-shell")).ToBeVisibleAsync();
            await Expect(page.Locator("[aria-label=\"Loading Planning\"]")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        }

        await WaitForFilledButtonBackgroundsToSettleAsync(page);
    }

    /// <summary>
    /// Seeds the browser theme preference before the next navigation.
    /// </summary>
    public static async Task SeedThemePreferenceAsync(IPage page, string preference)
    {
        await page.AddInitScriptAsync(
            $"localStorage.setItem('{ThemePreference.StorageKey}', '{preference}');");
    }

    private static void AssertNoBlockingViolations(IList<AxeResultItem>? violations, string context)
    {
        var blocking = (violations ?? [])
            .Where(violation => violation.Impact is "critical" or "serious")
            .ToList();

        if (blocking.Count == 0)
        {
            return;
        }

        var summary = string.Join(
            "\n\n",
            blocking.Select(violation =>
            {
                var nodes = string.Join(
                    "\n",
                    violation.Nodes.Take(5).Select(node => $"    - {string.Join(' ', node.Target)}"));
                return $"{violation.Id} ({violation.Impact}): {violation.Help}\n{nodes}";
            }));

        Assert.Fail($"Critical/serious accessibility violations on {context}:\n{summary}");
    }

    private static async Task WaitForFilledButtonBackgroundsToSettleAsync(IPage page)
    {
        var filledButtons = page.Locator("button.mud-button-filled:enabled");
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            var count = await filledButtons.CountAsync();
            if (count == 0)
            {
                return;
            }

            var settled = await filledButtons.EvaluateAllAsync<bool>(
                @"buttons => buttons.every(button => {
                    const backgroundColor = getComputedStyle(button).backgroundColor;
                    if (backgroundColor === 'transparent') return true;
                    const rgbaMatch = backgroundColor.match(/^rgba\(\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*,\s*([\d.]+)\s*\)$/);
                    if (rgbaMatch) return Number.parseFloat(rgbaMatch[1]) >= 1;
                    return /^rgb\(\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*\)$/.test(backgroundColor);
                })");

            if (settled)
            {
                return;
            }

            await Task.Delay(100);
        }
    }
}
