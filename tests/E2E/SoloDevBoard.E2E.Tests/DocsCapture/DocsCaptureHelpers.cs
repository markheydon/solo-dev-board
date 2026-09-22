using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.DocsCapture;

/// <summary>
/// Helpers for manual documentation screenshot capture against a local app with a real GitHub PAT.
/// </summary>
public static class DocsCaptureHelpers
{
    /// <summary>
    /// Canonical public Projects v2 board title used in documentation screenshots.
    /// </summary>
    public const string DocsExampleProjectBoard = "SoloDevBoard Roadmap";

    /// <summary>
    /// Canonical public repository used in documentation screenshots.
    /// </summary>
    public const string DocsExampleRepository = "markheydon/solo-dev-board";

    private static readonly Lazy<string> DocsImagesRootLazy = new(ResolveDocsImagesRoot);

    /// <summary>
    /// Repository-root-relative output directory for Hugo static images.
    /// </summary>
    public static string DocsImagesRoot => DocsImagesRootLazy.Value;

    /// <summary>
    /// Clears Planning browser settings so captures do not reuse a previous planning board.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task ClearPlanningLocalSettingsAsync(IPage page)
    {
        await page.AddInitScriptAsync(
            "window.localStorage.removeItem('solo-dev-board.planning-settings');");
    }

    /// <summary>
    /// Selects a planning board by visible title when the option exists.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="boardTitle">Exact board title to select.</param>
    /// <returns>True when the board was selected.</returns>
    public static async Task<bool> SelectPlanningBoardByTitleAsync(
        IPage page,
        string boardTitle = DocsExampleProjectBoard)
    {
        var boardSelect = page.GetByRole(AriaRole.Combobox, new() { Name = "Planning board" });
        await Expect(boardSelect).ToBeEnabledAsync(new() { Timeout = 60_000 });
        await boardSelect.ClickAsync();

        var preferredOption = page.GetByRole(
            AriaRole.Option,
            new() { NameRegex = new Regex("SoloDevBoard Roadmap", RegexOptions.IgnoreCase) });

        try
        {
            await Expect(preferredOption).ToBeVisibleAsync(new() { Timeout = 5_000 });
            await preferredOption.ClickAsync();
            await Expect(boardSelect).ToContainTextAsync(
                new Regex("SoloDevBoard Roadmap", RegexOptions.IgnoreCase),
                new() { Timeout = 15_000 });
            return true;
        }
        catch (PlaywrightException)
        {
            // Close the list without choosing a personal board when the canonical example is unavailable.
            await page.Keyboard.PressAsync("Escape");
            return false;
        }
    }

    /// <summary>
    /// Seeds light theme preference and opens a feature page ready for screenshot capture.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="route">Absolute path within the app (for example <c>/repositories</c>).</param>
    public static async Task OpenFeatureForCaptureAsync(IPage page, string route)
    {
        await page.AddInitScriptAsync(
            """
            // Prefer light colour scheme for consistent documentation screenshots.
            Object.defineProperty(window, 'matchMedia', {
              writable: true,
              value: (query) => ({
                matches: query.includes('prefers-color-scheme: dark') ? false : false,
                media: query,
                onchange: null,
                addListener: () => undefined,
                removeListener: () => undefined,
                addEventListener: () => undefined,
                removeEventListener: () => undefined,
                dispatchEvent: () => false,
              }),
            });
            """);

        await page.GotoAsync("/");
        await AccessibilityHelpers.SeedThemePreferenceAsync(page, "light");
        await page.GotoAsync(route);
        await Expect(page.Locator("#main-content")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        // Allow Blazor Server and MudBlazor to settle before interacting.
        await page.WaitForTimeoutAsync(1_500);
    }

    /// <summary>
    /// Waits for a repository autocomplete control to be ready for selection.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="autocompleteTestId">data-testid of the MudAutocomplete input.</param>
    public static async Task WaitForRepositoryAutocompleteReadyAsync(IPage page, string autocompleteTestId)
    {
        var autocomplete = page.GetByTestId(autocompleteTestId);
        await Expect(autocomplete).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(autocomplete).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    /// <summary>
    /// Selects a repository via the shared MudAutocomplete repository selector.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="autocompleteTestId">data-testid of the MudAutocomplete input.</param>
    /// <param name="repositoryFullName">Full repository name (for example <c>owner/repo</c>).</param>
    public static async Task SelectRepositoryInAutocompleteAsync(
        IPage page,
        string autocompleteTestId,
        string repositoryFullName = DocsExampleRepository)
    {
        await WaitForRepositoryAutocompleteReadyAsync(page, autocompleteTestId);

        var autocomplete = page.GetByTestId(autocompleteTestId);
        var searchTerm = repositoryFullName.Contains('/', StringComparison.Ordinal)
            ? repositoryFullName.Split('/').LastOrDefault() ?? repositoryFullName
            : repositoryFullName;

        await autocomplete.ClickAsync();
        await autocomplete.FillAsync(searchTerm);
        await page.WaitForTimeoutAsync(750);

        var option = page.GetByRole(AriaRole.Option, new() { Name = repositoryFullName, Exact = true });
        await Expect(option).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await option.ClickAsync();
        await page.WaitForTimeoutAsync(750);
    }

    /// <summary>
    /// Collapses the app navigation drawer so feature content fills the viewport.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task CollapseNavigationDrawerAsync(IPage page)
    {
        var drawer = page.Locator("#nav-drawer");
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation drawer" });
        await Expect(toggle).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Desktop layouts keep the drawer open by default; collapse before capture.
        var drawerBox = await drawer.BoundingBoxAsync();
        if (drawerBox is { Width: > 0 })
        {
            await toggle.ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }
    }

    /// <summary>
    /// Captures a full-page screenshot into the Hugo static images tree.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    /// <param name="featureSlug">Feature folder name under <c>website/static/images/</c>.</param>
    /// <param name="fileName">Kebab-case PNG file name, including the <c>.png</c> extension.</param>
    public static async Task CaptureDocsScreenshotAsync(IPage page, string featureSlug, string fileName)
    {
        await CollapseNavigationDrawerAsync(page);

        var targetDirectory = Path.Combine(DocsImagesRoot, featureSlug);
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = targetPath,
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
        });
    }

    /// <summary>
    /// Prepares the Audit Dashboard with loaded KPI and health indicator data.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareAuditDashboardForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/audit-dashboard");
        await SelectRepositoryInAutocompleteAsync(page, "audit-repository-autocomplete");

        var loadButton = page.GetByTestId("audit-load-selected-button");
        await Expect(loadButton).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await loadButton.ClickAsync();

        await Expect(page.GetByTestId("audit-kpi-summary-cards")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(page.GetByTestId("audit-summary-table")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares the Repositories page with a populated grid filtered to the example repository.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareRepositoriesForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/repositories");
        await Expect(page.GetByTestId("repositories-grid")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var searchField = page.GetByLabel("Search repositories");
        await searchField.FillAsync("solo-dev-board");
        await page.WaitForTimeoutAsync(750);
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "solo-dev-board" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    /// <summary>
    /// Prepares the Label Manager with labels loaded for the example repository.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareLabelManagerForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/labels");
        await SelectRepositoryInAutocompleteAsync(page, "repository-autocomplete");

        var loadButton = page.GetByTestId("load-labels-button");
        await Expect(loadButton).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await loadButton.ClickAsync();

        await Expect(page.GetByTestId("labels-grid")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares Recommended taxonomy Preview with remove-outside on so Remap extras is visible.
    /// Preview only — does not apply taxonomy.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareLabelManagerRemapPreviewForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/labels");
        await SelectRepositoryInAutocompleteAsync(page, "repository-autocomplete");

        await page.GetByRole(AriaRole.Tab, new() { Name = "Recommended taxonomy" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Apply recommended taxonomy" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        var removeOutsideCheckbox = page.GetByTestId("remove-labels-outside-taxonomy-checkbox");
        await Expect(removeOutsideCheckbox).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await removeOutsideCheckbox.ClickAsync();
        await Expect(page.GetByTestId("keep-area-labels-checkbox")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var previewButton = page.GetByTestId("preview-taxonomy-button");
        await Expect(previewButton).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await previewButton.ClickAsync();

        await Expect(page.GetByTestId("taxonomy-preview-card")).ToBeVisibleAsync(new() { Timeout = 90_000 });
        await Expect(page.GetByTestId("labels-recommended-remap-table")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares One-Click Migration with the example repository selected and Project board columns enabled.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareMigrationForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/migrate");
        await SelectRepositoryInAutocompleteAsync(page, "migration-repository-autocomplete");
        await Expect(page.GetByTestId("selected-repositories"))
            .ToContainTextAsync(DocsExampleRepository, new() { Timeout = 15_000 });

        var columnsSwitch = page.GetByTestId("migration-scope-columns-switch");
        await Expect(columnsSwitch).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await columnsSwitch.ClickAsync();
        await Expect(page.GetByText("Choose the source board whose Status columns are copied"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(750);
    }

    /// <summary>
    /// Prepares Planning — Daily Focus with the SoloDevBoard Roadmap board selected when available.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PreparePlanningDailyFocusForCaptureAsync(IPage page)
    {
        await PreparePlanningTabForCaptureAsync(
            page,
            "/planning/daily-focus",
            [
                "planning-daily-focus-no-board",
                "planning-daily-focus-board-state",
                "planning-daily-focus-error",
                "planning-daily-focus-empty",
            ]);
    }

    /// <summary>
    /// Prepares Planning Backlog Review with the SoloDevBoard Roadmap board selected when available.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PreparePlanningBacklogForCaptureAsync(IPage page)
    {
        await PreparePlanningTabForCaptureAsync(
            page,
            "/planning/backlog",
            [
                "planning-backlog-no-board",
                "planning-backlog-filters",
                "planning-backlog-panels",
                "planning-backlog-error",
            ]);
    }

    /// <summary>
    /// Prepares Planning Iteration Planning with the SoloDevBoard Roadmap board selected when available.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PreparePlanningIterationForCaptureAsync(IPage page)
    {
        await PreparePlanningTabForCaptureAsync(
            page,
            "/planning/iteration",
            [
                "planning-planning-no-board",
                "planning-planning-up-next",
                "planning-planning-candidates",
                "planning-planning-up-next-empty",
                "planning-planning-candidates-empty",
                "planning-planning-error",
                "planning-planning-partial-failure",
            ]);
    }

    /// <summary>
    /// Prepares Planning Repo Management showing thresholds and participation regions.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PreparePlanningReposForCaptureAsync(IPage page)
    {
        await ClearPlanningLocalSettingsAsync(page);
        await OpenFeatureForCaptureAsync(page, "/planning/repos");
        await Expect(page.GetByTestId("planning-shell")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var chromeError = page.GetByTestId("planning-chrome-error");
        var thresholdsRegion = page.GetByTestId("planning-thresholds-region");
        await Expect(chromeError.Or(thresholdsRegion).First).ToBeVisibleAsync(new() { Timeout = 60_000 });

        if (await thresholdsRegion.IsVisibleAsync())
        {
            try
            {
                await SelectPlanningBoardByTitleAsync(page);
            }
            catch (PlaywrightException)
            {
                // Board selection is best-effort for documentation capture.
            }

            await Expect(page.GetByTestId("planning-participation-region"))
                .ToBeVisibleAsync(new() { Timeout = 30_000 });

            var includedFilter = page.GetByTestId("planning-included-filter");
            if (await includedFilter.IsVisibleAsync())
            {
                await includedFilter.FillAsync("solo-dev-board");
                await page.WaitForTimeoutAsync(500);
            }

            await Expect(page.GetByTestId("planning-repository-summary-region"))
                .ToBeVisibleAsync(new() { Timeout = 30_000 });
        }

        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares the Board Rules Visualiser with board context loaded for the example repository.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareBoardRulesForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/board-rules");
        await SelectRepositoryInAutocompleteAsync(page, "board-rules-repository-autocomplete");

        var projectBoardSelect = page
            .GetByTestId("board-rules-project-board-selector")
            .GetByRole(AriaRole.Combobox, new() { Name = "Project board" });

        await Expect(projectBoardSelect).ToBeEnabledAsync(new() { Timeout = 60_000 });
        await projectBoardSelect.ClickAsync();

        var preferredOption = page.GetByRole(
            AriaRole.Option,
            new() { NameRegex = new Regex("SoloDevBoard Roadmap", RegexOptions.IgnoreCase) });
        var boardOption = await preferredOption.CountAsync() > 0
            ? preferredOption.First
            : page.GetByRole(AriaRole.Option).First;

        await Expect(boardOption).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var boardTitle = (await boardOption.TextContentAsync())?.Trim();
        await boardOption.ClickAsync();

        if (!string.IsNullOrEmpty(boardTitle))
        {
            await Expect(projectBoardSelect).ToContainTextAsync(boardTitle, new() { Timeout = 15_000 });
        }

        await Expect(page.GetByTestId("board-rules-board-context-ready-state"))
            .ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares the Triage UI with an active session showing the first queue item.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareTriageForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/triage");
        await SelectRepositoryInAutocompleteAsync(page, "triage-repository-autocomplete");

        var startButton = page.GetByTestId("triage-start-session-button");
        await Expect(startButton).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await startButton.ClickAsync();

        var sessionComplete = page.GetByTestId("triage-session-complete-region");
        var itemDetail = page.GetByTestId("triage-item-detail-region");
        await Expect(sessionComplete.Or(itemDetail)).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1_000);
    }

    /// <summary>
    /// Prepares Actions Templates with the custom source section populated, the example repository
    /// selected as an apply target, and a built-in template open in the detail panel.
    /// Does not click Load templates, so the grid stays on built-ins for a readable overview.
    /// </summary>
    /// <param name="page">Playwright page.</param>
    public static async Task PrepareActionsTemplatesForCaptureAsync(IPage page)
    {
        await OpenFeatureForCaptureAsync(page, "/actions-templates");

        await Expect(page.GetByTestId("actions-templates-custom-source-region"))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.GetByTestId("actions-templates-custom-source-field"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        await SelectRepositoryInAutocompleteAsync(page, "workflow-repository-autocomplete");
        await SelectRepositoryInAutocompleteAsync(page, "actions-templates-custom-source-autocomplete");

        await Expect(page.GetByTestId("actions-templates-grid")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var firstTemplateCard = page.Locator("[data-testid^=\"actions-templates-card-\"]").First;
        await Expect(firstTemplateCard).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await firstTemplateCard.GetByRole(AriaRole.Button).First.ClickAsync();

        await Expect(page.GetByTestId("actions-templates-yaml-preview"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(750);
    }

    private static async Task PreparePlanningTabForCaptureAsync(
        IPage page,
        string route,
        IReadOnlyList<string> readyLocatorTestIds)
    {
        await ClearPlanningLocalSettingsAsync(page);
        await OpenFeatureForCaptureAsync(page, route);
        await page.EvaluateAsync("() => { window.localStorage.removeItem('solo-dev-board.planning-settings'); }");
        await page.ReloadAsync();
        await Expect(page.GetByTestId("planning-shell")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.GetByTestId("planning-shared-chrome")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var chromeError = page.GetByTestId("planning-chrome-error");
        var ready = chromeError;
        foreach (var testId in readyLocatorTestIds)
        {
            ready = ready.Or(page.GetByTestId(testId));
        }

        await Expect(ready.First).ToBeVisibleAsync(new() { Timeout = 90_000 });
        if (await chromeError.IsVisibleAsync())
        {
            return;
        }

        var selected = await SelectPlanningBoardByTitleAsync(page);
        if (selected)
        {
            await Expect(ready.First).ToBeVisibleAsync(new() { Timeout = 90_000 });
            await page.WaitForTimeoutAsync(3_000);
        }

        await page.WaitForTimeoutAsync(1_000);
    }

    private static string ResolveDocsImagesRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SoloDevBoard.slnx")))
            {
                return Path.Combine(directory.FullName, "website", "static", "images");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing SoloDevBoard.slnx.");
    }
}
