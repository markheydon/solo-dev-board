using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Board Rules Visualiser shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class BoardRulesTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the selector region and compare mode toggle render before repository data is available.
    /// </summary>
    [Fact]
    public async Task SelectorRegionAndCompareModeToggle_RenderBeforeRepositoryDataIsAvailable()
    {
        await Page.GotoAsync("/board-rules");

        await Expect(Page).ToHaveTitleAsync(new Regex("Board Rules Visualiser"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Board Rules Visualiser" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("board-rules-selector-region")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("board-rules-compare-mode-toggle")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("board-rules-visualisation-region")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies repository load failure surfaces in the board rules feedback region.
    /// </summary>
    [Fact]
    public async Task RepositoryLoadFailure_SurfacesInBoardRulesFeedbackRegion()
    {
        await Page.GotoAsync("/board-rules");

        await Expect(Page.GetByTestId("board-rules-repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("board-rules-error-alert")).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex("GitHub API request failed", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("board-rules-reload-repositories-button")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the selector header exposes reload from GitHub and compare mode after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task SelectorHeader_ExposesReloadFromGitHubAndCompareModeAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/board-rules");

        await Expect(Page.GetByTestId("board-rules-repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });

        var selectorRegion = Page.GetByTestId("board-rules-selector-region");
        await Expect(selectorRegion).ToBeVisibleAsync();
        await Expect(selectorRegion.GetByTestId("board-rules-compare-mode-toggle")).ToBeVisibleAsync();
        await Expect(selectorRegion.GetByTestId("board-rules-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(selectorRegion.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }
}
