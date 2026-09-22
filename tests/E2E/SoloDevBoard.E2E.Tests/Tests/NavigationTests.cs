using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Feature navigation tests for home cards and the shell drawer.
/// </summary>
[Trait("Category", "E2E")]
public sealed class NavigationTests : SoloDevBoardPageTest
{
    private static readonly (string LinkName, string Path, Regex Title)[] FeatureCards =
    [
        ("Open Audit Dashboard", "/audit-dashboard", new Regex("Audit Dashboard")),
        ("Open Repositories", "/repositories", new Regex("Repositories")),
        ("Open One-Click Migration", "/migrate", new Regex("One-Click Migration")),
        ("Open Label Manager", "/labels", new Regex("Label Manager")),
        ("Open Board Rules Visualiser", "/board-rules", new Regex("Board Rules Visualiser")),
        ("Open Triage UI", "/triage", new Regex("Triage UI")),
        ("Open Actions Templates", "/actions-templates", new Regex("Actions Templates")),
        ("Open Planning", "/planning/daily-focus", new Regex("Planning — Daily Focus")),
    ];

    /// <summary>
    /// Verifies the home dashboard lists all feature entry points.
    /// </summary>
    [Fact]
    public async Task HomeDashboard_DisplaysAllFeatureEntryPoints()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Home" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Audit Dashboard" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Repositories" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open One-Click Migration" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Label Manager" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Board Rules Visualiser" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Triage UI" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Actions Templates" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open Planning" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies home feature cards navigate to the matching feature page.
    /// </summary>
    [Fact]
    public async Task HomeFeatureCards_NavigateToMatchingFeaturePage()
    {
        foreach (var (linkName, path, title) in FeatureCards)
        {
            await Page.GotoAsync("/");
            await Page.GetByRole(AriaRole.Link, new() { Name = linkName }).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex($"{path}$"));
            await Expect(Page).ToHaveTitleAsync(title);
        }
    }

    /// <summary>
    /// Verifies drawer navigation reaches each primary feature route.
    /// </summary>
    [Fact]
    public async Task DrawerNavigation_ReachesEachPrimaryFeatureRoute()
    {
        await Page.GotoAsync("/");

        foreach (var feature in NavigationHelpers.FeatureRoutes)
        {
            await NavigationHelpers.NavigateViaDrawerAsync(Page, feature.NavLabel);
            await Expect(Page).ToHaveURLAsync(new Regex($"{feature.Path}$"));
            await Expect(Page).ToHaveTitleAsync(new Regex(feature.TitlePattern));
        }
    }
}
