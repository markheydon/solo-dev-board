using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Planning;
using SoloDevBoard.App.Components.Features.Planning.Pages;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Planning board setup compatibility chrome.</summary>
public sealed class PlanningBoardSetupTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPlanningProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPlanningProjectBoardDiscoveryService>();
    private readonly IDailyFocusBoardStateService _boardStateService = Substitute.For<IDailyFocusBoardStateService>();
    private readonly IDailyFocusStalledReviewService _stalledReviewService = Substitute.For<IDailyFocusStalledReviewService>();
    private readonly IDailyFocusRecommendationService _recommendationService = Substitute.For<IDailyFocusRecommendationService>();
    private readonly FakePlanningSettingsStorage _settingsStorage = new()
    {
        StoredJson = """{"planningBoardNodeId":"PVT_board","capacity":8,"stallDays":3,"neglectDays":14,"excludedRepositories":[]}""",
    };

    [Fact]
    public async Task PlanningShell_WhenBoardHasCompatibilityIssues_ShowsBoardSetupTabAndSummary()
    {
        ConfigureDefaults();
        var compatibilityService = PlanningTestChromeDependencies.CreateBoardCompatibilityService(_ => new PlanningBoardCompatibilityReportDto(
            "PVT_board",
            [
                new PlanningBoardCompatibilityIssueDto(
                    "missing-status-todo",
                    PlanningBoardCompatibilitySeverity.Error,
                    "Missing Todo status column",
                    "Iteration Planning uses a Status option named Todo for Re-commit and Remove."),
            ]));

        await using var ctx = CreateContext(compatibilityService);
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Board setup", cut.Markup);
            Assert.Contains("data-testid=\"planning-board-compatibility-summary\"", cut.Markup);
            Assert.Contains("Missing Todo status column", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningBoardSetup_WhenIssuesExist_ShowsGroupedPanels()
    {
        ConfigureDefaults();
        var compatibilityService = PlanningTestChromeDependencies.CreateBoardCompatibilityService(_ => new PlanningBoardCompatibilityReportDto(
            "PVT_board",
            [
                new PlanningBoardCompatibilityIssueDto(
                    "missing-status-todo",
                    PlanningBoardCompatibilitySeverity.Error,
                    "Missing Todo status column",
                    "Add a Todo column on the project board."),
                new PlanningBoardCompatibilityIssueDto(
                    "missing-focus-order-field",
                    PlanningBoardCompatibilitySeverity.Warning,
                    "Missing Focus Order field",
                    "Iteration Planning can still move items to Up Next."),
            ]));

        await using var ctx = CreateContext(compatibilityService);
        ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/planning/board-setup");
        var cut = ctx.RenderPlanningPage<PlanningBoardSetup>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-board-setup-page\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-board-setup-errors-panel\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-board-setup-warnings-panel\"", cut.Markup);
            Assert.Contains("Missing Todo status column", cut.Markup);
            Assert.Contains("Missing Focus Order field", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningBoardSetup_WhenRecheckClicked_ReloadsCompatibilityFromGitHub()
    {
        ConfigureDefaults();
        var compatibilityService = Substitute.For<IPlanningBoardCompatibilityService>();
        compatibilityService
            .GetReportAsync("PVT_board", false, Arg.Any<CancellationToken>())
            .Returns(new PlanningBoardCompatibilityReportDto(
                "PVT_board",
                [
                    new PlanningBoardCompatibilityIssueDto(
                        "missing-status-todo",
                        PlanningBoardCompatibilitySeverity.Error,
                        "Missing Todo status column",
                        "Add a Todo column on the project board."),
                ]));
        compatibilityService
            .GetReportAsync("PVT_board", true, Arg.Any<CancellationToken>())
            .Returns(new PlanningBoardCompatibilityReportDto("PVT_board", []));

        await using var ctx = CreateContext(compatibilityService);
        ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/planning/board-setup");
        var cut = ctx.RenderPlanningPage<PlanningBoardSetup>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-board-setup-recheck\"", cut.Markup);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-board-setup-recheck']").Click());

        cut.WaitForAssertion(() =>
        {
            compatibilityService.Received(1).GetReportAsync("PVT_board", true, Arg.Any<CancellationToken>());
        });
    }

    private void ConfigureDefaults()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto(
                [new PlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            new DailyFocusBoardStateDto([], 0, 8, 0, [], 3));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>()).Returns(
            new DailyFocusStalledReviewSnapshotDto([], UsedInReviewColumn: false));
        _recommendationService.GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>()).Returns(
            new DailyFocusRecommendationResultDto([], []));
    }

    private BunitContext CreateContext(IPlanningBoardCompatibilityService compatibilityService)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddSingleton<IPlanningSettingsStorage>(_settingsStorage);
        ctx.Services.AddScoped<IPlanningSettingsService, PlanningSettingsService>();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _projectBoardDiscoveryService);
        ctx.Services.AddScoped(_ => _boardStateService);
        ctx.Services.AddScoped(_ => _stalledReviewService);
        ctx.Services.AddScoped(_ => _recommendationService);
        ctx.Services.AddScoped(_ => compatibilityService);
        ctx.Services.AddScoped<PlanningChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false);

    private sealed class FakePlanningSettingsStorage : IPlanningSettingsStorage
    {
        public string? StoredJson { get; set; }

        public Task<string?> GetStoredJsonAsync() => Task.FromResult(StoredJson);

        public Task SetStoredJsonAsync(string json)
        {
            StoredJson = json;
            return Task.CompletedTask;
        }
    }
}
