using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Planning feature shell tests across Daily Focus, Repos, Backlog, and Iteration routes.
/// </summary>
[Trait("Category", "E2E")]
public sealed class PlanningTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies drawer navigation opens the Daily Focus tab shell.
    /// </summary>
    [Fact]
    public async Task DrawerNavigation_OpensDailyFocusTabShell()
    {
        await Page.GotoAsync("/");

        await NavigationHelpers.NavigateViaDrawerAsync(Page, "Planning");

        await Expect(Page).ToHaveURLAsync(new Regex("/planning/daily-focus$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Planning — Daily Focus"));
        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-tab-strip")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Daily Focus" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-daily-focus-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Daily Focus" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies daily focus shows occupancy and recommendations, empty copy, or a load error.
    /// </summary>
    [Fact]
    public async Task DailyFocus_ShowsOccupancyAndRecommendationsEmptyCopyOrLoadError()
    {
        await Page.GotoAsync("/planning/daily-focus");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var noBoardAlert = Page.GetByTestId("planning-daily-focus-no-board");
        var occupancyRegion = Page.GetByTestId("planning-daily-focus-board-state");
        var occupancyError = Page.GetByTestId("planning-daily-focus-error");

        await Expect(chromeError.Or(noBoardAlert).Or(occupancyRegion).Or(occupancyError).First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await occupancyError.IsVisibleAsync())
        {
            await Expect(occupancyError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            await Expect(occupancyError).ToContainTextAsync("Unable to load board occupancy");
            return;
        }

        if (await occupancyRegion.IsVisibleAsync())
        {
            await Expect(Page.GetByTestId("planning-daily-focus-stalled")).ToBeVisibleAsync();
            var stalledReviewsRegion = Page.GetByTestId("planning-daily-focus-stalled-reviews");
            var stalledReviewsError = Page.GetByTestId("planning-daily-focus-stalled-reviews-error");
            await Expect(stalledReviewsRegion.Or(stalledReviewsError).First).ToBeVisibleAsync();
        }

        if (await occupancyRegion.IsVisibleAsync())
        {
            var recommendationsRegion = Page.GetByTestId("planning-daily-focus-recommendations");
            var recommendationsError = Page.GetByTestId("planning-daily-focus-recommendations-error");
            var recommendationsWarning = Page.GetByTestId("planning-daily-focus-recommendations-warning");
            await Expect(recommendationsRegion.Or(recommendationsError).Or(recommendationsWarning).First).ToBeVisibleAsync();

            if (await recommendationsRegion.IsVisibleAsync())
            {
                await Expect(Page.GetByTestId("planning-limit-recommendations-switch")).ToBeVisibleAsync();
                await Expect(
                    Page.GetByRole(AriaRole.Heading, new() { Name = "Recommended today (all included repositories)" })
                        .Or(Page.GetByRole(AriaRole.Heading, new() { Name = "Recommended today (selected planning board)" })))
                    .ToBeVisibleAsync();
            }
        }
    }

    /// <summary>
    /// Verifies the repos tab shows threshold and exclusion regions or a chrome error.
    /// </summary>
    [Fact]
    public async Task ReposTab_ShowsThresholdAndExclusionRegionsOrChromeError()
    {
        await Page.GotoAsync("/planning/repos");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var thresholdsRegion = Page.GetByTestId("planning-thresholds-region");
        var exclusionsRegion = Page.GetByTestId("planning-exclusions-region");

        await Expect(chromeError.Or(thresholdsRegion).First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        await Expect(thresholdsRegion).ToBeVisibleAsync();
        await Expect(exclusionsRegion).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-participation-summary")).ToBeVisibleAsync();
        await Expect(
            Page.GetByTestId("planning-included-table").Or(Page.GetByTestId("planning-no-included-text")))
            .ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-capacity-field")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-exclude-autocomplete")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-no-exclusions-text")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-repository-summary-region")).ToBeVisibleAsync();
        await Expect(
            Page.GetByTestId("planning-repository-summary-table")
                .Or(Page.GetByTestId("planning-repository-summary-empty"))
                .Or(Page.GetByTestId("planning-repository-summary-error"))
                .Or(Page.GetByTestId("planning-repository-summary-partial-failure"))
                .Or(Page.GetByTestId("planning-repository-summary-loading")))
            .ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the backlog tab shows filters and urgency panels, empty copy, or a load error.
    /// </summary>
    [Fact]
    public async Task BacklogTab_ShowsFiltersAndUrgencyPanelsEmptyCopyOrLoadError()
    {
        await Page.GotoAsync("/planning/backlog");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Backlog" })).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var noBoardAlert = Page.GetByTestId("planning-backlog-no-board");
        var filters = Page.GetByTestId("planning-backlog-filters");
        var loadError = Page.GetByTestId("planning-backlog-error");

        await Expect(chromeError.Or(noBoardAlert).Or(filters).Or(loadError).First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await loadError.IsVisibleAsync())
        {
            await Expect(loadError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            await Expect(loadError).ToContainTextAsync("Unable to load the backlog");
            return;
        }

        if (await filters.IsVisibleAsync())
        {
            await Expect(Page.GetByTestId("planning-backlog-panels")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-urgent")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-ready")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-blocked")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-search")).ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// Verifies direct navigation opens the Daily Focus route shell.
    /// </summary>
    [Fact]
    public async Task DirectNavigation_OpensDailyFocusRouteShell()
    {
        await Page.GotoAsync("/planning/daily-focus");

        await Expect(Page).ToHaveURLAsync(new Regex("/planning/daily-focus$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Planning — Daily Focus"));
        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-tab-strip")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Daily Focus" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-daily-focus-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Daily Focus" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies daily focus shows no-board instructional copy, empty board, or occupancy error.
    /// </summary>
    [Fact]
    public async Task DailyFocus_ShowsNoBoardInstructionalCopyEmptyBoardOrOccupancyError()
    {
        await Page.GotoAsync("/planning/daily-focus");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var noBoardAlert = Page.GetByTestId("planning-daily-focus-no-board");
        var occupancyRegion = Page.GetByTestId("planning-daily-focus-board-state");
        var emptyBoardAlert = Page.GetByTestId("planning-daily-focus-empty");
        var emptyBoardPaper = Page.GetByTestId("planning-daily-focus-empty-board");
        var occupancyError = Page.GetByTestId("planning-daily-focus-error");

        await Expect(
            chromeError
                .Or(noBoardAlert)
                .Or(occupancyRegion)
                .Or(emptyBoardAlert)
                .Or(emptyBoardPaper)
                .Or(occupancyError)
                .First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await occupancyError.IsVisibleAsync())
        {
            await Expect(occupancyError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            await Expect(occupancyError).ToContainTextAsync("Unable to load board occupancy");
            return;
        }

        if (await noBoardAlert.IsVisibleAsync())
        {
            await Expect(noBoardAlert).ToContainTextAsync(
                "Select a planning board in the dropdown above to load Daily Focus occupancy and recommendations.");
            return;
        }

        if (await occupancyRegion.IsVisibleAsync())
        {
            var hasEmptyBoardState = await emptyBoardAlert.Or(emptyBoardPaper).First.IsVisibleAsync();
            if (!hasEmptyBoardState)
            {
                return;
            }
        }

        await Expect(emptyBoardAlert.Or(emptyBoardPaper).First).ToBeVisibleAsync();
        await Expect(
            Page.GetByText("This planning board has no items.")
                .Or(Page.GetByText("This planning board has no Status options and no items yet.")))
            .ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the repos tab shows planning thresholds, exclusions, and per-repository summary shell.
    /// </summary>
    [Fact]
    public async Task ReposTab_ShowsPlanningThresholdsExclusionsAndPerRepositorySummaryShell()
    {
        await Page.GotoAsync("/planning/repos");

        await Expect(Page).ToHaveTitleAsync(new Regex("Planning — Repos"));
        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-repos-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Repo Management" })).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var thresholdsRegion = Page.GetByTestId("planning-thresholds-region");
        var exclusionsRegion = Page.GetByTestId("planning-exclusions-region");
        var noBoardAlert = Page.GetByTestId("planning-no-board-alert");

        await Expect(chromeError.Or(thresholdsRegion).First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        await Expect(thresholdsRegion).ToBeVisibleAsync();
        await Expect(exclusionsRegion).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-participation-summary")).ToBeVisibleAsync();
        await Expect(
            Page.GetByTestId("planning-included-table").Or(Page.GetByTestId("planning-no-included-text")))
            .ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-capacity-field")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-stall-days-field")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-neglect-days-field")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-exclude-autocomplete")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-no-exclusions-text")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-repository-summary-region")).ToBeVisibleAsync();
        await Expect(
            Page.GetByTestId("planning-repository-summary-table")
                .Or(Page.GetByTestId("planning-repository-summary-empty"))
                .Or(Page.GetByTestId("planning-repository-summary-error"))
                .Or(Page.GetByTestId("planning-repository-summary-partial-failure"))
                .Or(Page.GetByTestId("planning-repository-summary-loading")))
            .ToBeVisibleAsync();

        if (await noBoardAlert.IsVisibleAsync())
        {
            await Expect(noBoardAlert).ToContainTextAsync("Select a planning board");
        }
        else if (await Page.GetByText(new Regex("Board:")).IsVisibleAsync())
        {
            await Expect(Page.GetByTestId("planning-shared-chrome")).ToContainTextAsync("Board:");
        }
        else
        {
            await Expect(Page.GetByRole(AriaRole.Combobox, new() { Name = "Planning board" })).ToBeAttachedAsync();
        }
    }

    /// <summary>
    /// Verifies direct navigation opens the Planning iteration route shell.
    /// </summary>
    [Fact]
    public async Task DirectNavigation_OpensPlanningIterationRouteShell()
    {
        await Page.GotoAsync("/planning/iteration");

        await Expect(Page).ToHaveURLAsync(new Regex("/planning/iteration$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Planning — Iteration"));
        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-tab-strip")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Iteration" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-planning-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Iteration Planning" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies iteration shows no-board instructional copy, Up Next and candidates, empty copy, or load error.
    /// </summary>
    [Fact]
    public async Task Iteration_ShowsNoBoardInstructionalCopyUpNextAndCandidatesEmptyCopyOrLoadError()
    {
        await Page.GotoAsync("/planning/iteration");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var noBoardAlert = Page.GetByTestId("planning-planning-no-board");
        var upNextRegion = Page.GetByTestId("planning-planning-up-next");
        var candidatesRegion = Page.GetByTestId("planning-planning-candidates");
        var upNextEmpty = Page.GetByTestId("planning-planning-up-next-empty");
        var candidatesEmpty = Page.GetByTestId("planning-planning-candidates-empty");
        var loadError = Page.GetByTestId("planning-planning-error");
        var partialFailure = Page.GetByTestId("planning-planning-partial-failure");

        await Expect(
            chromeError
                .Or(noBoardAlert)
                .Or(upNextRegion)
                .Or(candidatesRegion)
                .Or(upNextEmpty)
                .Or(candidatesEmpty)
                .Or(loadError)
                .Or(partialFailure)
                .First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await loadError.IsVisibleAsync())
        {
            await Expect(loadError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await noBoardAlert.IsVisibleAsync())
        {
            await Expect(noBoardAlert).ToContainTextAsync(
                "Select a planning board in the dropdown above to load Up Next and candidate work items.");
            return;
        }

        if (await partialFailure.IsVisibleAsync())
        {
            await Expect(partialFailure).ToContainTextAsync("failed");
        }

        if (await upNextRegion.IsVisibleAsync())
        {
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "This batch (Up Next)" })).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-planning-capacity")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-planning-active-load")).ToBeVisibleAsync();
            await Expect(upNextEmpty.Or(Page.GetByTestId("planning-planning-up-next-row")).First).ToBeVisibleAsync();
        }

        if (await candidatesRegion.IsVisibleAsync())
        {
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Candidate picker" })).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-planning-search")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-planning-show-issues")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-planning-show-prs")).ToBeVisibleAsync();
            await Expect(
                candidatesEmpty.Or(Page.GetByTestId("planning-planning-add-button")).First)
                .ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// Verifies the iteration shell exposes stall gate and pause line test ids when stalled Up Next is present.
    /// </summary>
    [Fact]
    public async Task IterationShell_ExposesStallGateAndPauseLineTestIdsWhenStalledUpNextIsPresent()
    {
        await Page.GotoAsync("/planning/iteration");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var upNextRegion = Page.GetByTestId("planning-planning-up-next");
        var stallGateAlert = Page.GetByTestId("planning-planning-stall-gate-alert");
        var candidatePauseLine = Page.GetByTestId("planning-planning-candidate-pause-line");

        if (await upNextRegion.IsVisibleAsync())
        {
            await Expect(Page.GetByTestId("planning-planning-capacity")).ToBeVisibleAsync();

            if (await stallGateAlert.IsVisibleAsync())
            {
                await Expect(stallGateAlert).ToBeVisibleAsync();
                await Expect(candidatePauseLine).ToBeVisibleAsync();
                await Expect(Page.GetByTestId("planning-planning-add-button")).ToBeDisabledAsync();
            }
        }
    }

    /// <summary>
    /// Verifies direct navigation opens the Backlog Review route shell.
    /// </summary>
    [Fact]
    public async Task DirectNavigation_OpensBacklogReviewRouteShell()
    {
        await Page.GotoAsync("/planning/backlog");

        await Expect(Page).ToHaveURLAsync(new Regex("/planning/backlog$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Planning — Backlog"));
        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-tab-strip")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Backlog" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("planning-backlog-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Backlog Review" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies backlog shows no-board instructional copy, grouping panels, empty catalogue, or load error.
    /// </summary>
    [Fact]
    public async Task Backlog_ShowsNoBoardInstructionalCopyGroupingPanelsEmptyCatalogueOrLoadError()
    {
        await Page.GotoAsync("/planning/backlog");

        await Expect(Page.GetByTestId("planning-shell")).ToBeVisibleAsync();

        var chromeError = Page.GetByTestId("planning-chrome-error");
        var noBoardAlert = Page.GetByTestId("planning-backlog-no-board");
        var filters = Page.GetByTestId("planning-backlog-filters");
        var panels = Page.GetByTestId("planning-backlog-panels");
        var catalogueEmpty = Page.GetByTestId("planning-backlog-empty");
        var filterEmpty = Page.GetByTestId("planning-backlog-filter-empty");
        var loadError = Page.GetByTestId("planning-backlog-error");
        var warning = Page.GetByTestId("planning-backlog-warning");

        await Expect(
            chromeError
                .Or(noBoardAlert)
                .Or(filters)
                .Or(catalogueEmpty)
                .Or(filterEmpty)
                .Or(loadError)
                .Or(warning)
                .First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await chromeError.IsVisibleAsync())
        {
            await Expect(chromeError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            return;
        }

        if (await loadError.IsVisibleAsync())
        {
            await Expect(loadError.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
            await Expect(loadError).ToContainTextAsync("Unable to load the backlog");
            return;
        }

        if (await warning.IsVisibleAsync())
        {
            await Expect(warning).ToContainTextAsync("failed to load");
            await Expect(Page.GetByTestId("planning-backlog-retry")).ToBeVisibleAsync();
        }

        if (await noBoardAlert.IsVisibleAsync())
        {
            await Expect(noBoardAlert).ToContainTextAsync(
                "Select a planning board in the dropdown above to load Backlog Review groups.");
            return;
        }

        if (await catalogueEmpty.IsVisibleAsync())
        {
            await Expect(catalogueEmpty).ToContainTextAsync("No open issues or pull requests in included repositories.");
            return;
        }

        if (await filterEmpty.IsVisibleAsync())
        {
            await Expect(filterEmpty).ToContainTextAsync("No items match the current filters.");
            return;
        }

        if (await filters.IsVisibleAsync())
        {
            await Expect(panels).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-urgent")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-ready")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-awaiting-triage")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-blocked")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-epics")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-neglected")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-search")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-type-filter")).ToBeVisibleAsync();
            await Expect(Page.GetByTestId("planning-backlog-repo-filter")).ToBeVisibleAsync();
        }
    }
}
