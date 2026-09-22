using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.DocsCapture;

/// <summary>
/// Manual documentation screenshot suite.
/// Prerequisites:
/// - SoloDevBoard running locally with a real GitHub PAT.
/// - DocsCapture:Enabled=true (public-only catalogues).
/// - <c>DOCS_CAPTURE_ENABLED=1</c> to opt in to this test class.
///
/// Composition rules: see plan/DOCS_STRATEGY.md#screenshot-composition.
/// </summary>
[Trait("Category", "DocsCapture")]
public sealed class FeatureScreenshotTests : SoloDevBoardPageTest
{
    private const string SkipReason =
        "Docs capture is disabled. Set DOCS_CAPTURE_ENABLED=1 to run manual documentation screenshot tests.";

    /// <summary>
    /// Captures the Audit Dashboard with loaded KPI summary.
    /// </summary>
    [Fact]
    public async Task Capture_AuditDashboardWithLoadedKpiSummary()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareAuditDashboardForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "audit-dashboard", "overview.png");
    }

    /// <summary>
    /// Captures the Repositories page with a populated grid.
    /// </summary>
    [Fact]
    public async Task Capture_RepositoriesWithPopulatedGrid()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareRepositoriesForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "repositories", "overview.png");
    }

    /// <summary>
    /// Captures the Label Manager with a loaded labels grid.
    /// </summary>
    [Fact]
    public async Task Capture_LabelManagerWithLoadedLabelsGrid()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareLabelManagerForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "label-manager", "overview.png");
    }

    /// <summary>
    /// Captures the Label Manager Recommended taxonomy remap extras preview.
    /// </summary>
    [Fact]
    public async Task Capture_LabelManagerRecommendedTaxonomyRemapExtrasPreview()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareLabelManagerRemapPreviewForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "label-manager", "remap-extras.png");
    }

    /// <summary>
    /// Captures One-Click Migration with the example repository selected.
    /// </summary>
    [Fact]
    public async Task Capture_OneClickMigrationWithExampleRepositorySelected()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareMigrationForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "one-click-migration", "overview.png");
    }

    /// <summary>
    /// Captures the Board Rules Visualiser with board context loaded.
    /// </summary>
    [Fact]
    public async Task Capture_BoardRulesVisualiserWithBoardContextLoaded()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareBoardRulesForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "board-rules-visualiser", "overview.png");
    }

    /// <summary>
    /// Captures the Triage UI with an active session.
    /// </summary>
    [Fact]
    public async Task Capture_TriageUiWithActiveSession()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareTriageForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "triage-ui", "overview.png");
    }

    /// <summary>
    /// Captures Actions Templates with template detail open.
    /// </summary>
    [Fact]
    public async Task Capture_ActionsTemplatesWithTemplateDetailOpen()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PrepareActionsTemplatesForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "actions-templates", "overview.png");
    }

    /// <summary>
    /// Captures Planning — Daily Focus with board occupancy.
    /// </summary>
    [Fact]
    public async Task Capture_PlanningDailyFocusWithBoardOccupancy()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PreparePlanningDailyFocusForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "planning", "daily-focus.png");
    }

    /// <summary>
    /// Captures Planning Backlog Review with urgency panels.
    /// </summary>
    [Fact]
    public async Task Capture_PlanningBacklogReviewWithUrgencyPanels()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PreparePlanningBacklogForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "planning", "backlog.png");
    }

    /// <summary>
    /// Captures Planning Iteration with Up Next and candidates.
    /// </summary>
    [Fact]
    public async Task Capture_PlanningIterationWithUpNextAndCandidates()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PreparePlanningIterationForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "planning", "iteration.png");
    }

    /// <summary>
    /// Captures Planning Repo Management thresholds and participation.
    /// </summary>
    [Fact]
    public async Task Capture_PlanningRepoManagementThresholdsAndParticipation()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.PreparePlanningReposForCaptureAsync(Page);
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "planning", "repos.png");
    }

    /// <summary>
    /// Captures the Home dashboard.
    /// </summary>
    [Fact]
    public async Task Capture_HomeDashboard()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.OpenFeatureForCaptureAsync(Page, "/");
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "dashboard", "home.png");
    }

    /// <summary>
    /// Captures the About page.
    /// </summary>
    [Fact]
    public async Task Capture_AboutPage()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.OpenFeatureForCaptureAsync(Page, "/about");
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "about", "overview.png");
    }

    /// <summary>
    /// Captures the Appearance theme control in the app bar.
    /// </summary>
    [Fact]
    public async Task Capture_AppearanceThemeControlInAppBar()
    {
        Skip.If(!DocsCaptureEnvironment.IsEnabled, SkipReason);
        await DocsCaptureHelpers.OpenFeatureForCaptureAsync(Page, "/");
        // Theme toggle lives in the app bar on every page; capture home with the control visible.
        await DocsCaptureHelpers.CaptureDocsScreenshotAsync(Page, "appearance", "theme-toggle.png");
    }
}
