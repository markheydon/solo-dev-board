using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.PmWorkflow;
using SoloDevBoard.App.Components.Features.PmWorkflow.Pages;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Iteration Planning on <see cref="PmWorkflowPlanning"/>.</summary>
public sealed class PmWorkflowPlanningTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IIterationPlanningService _planningService = Substitute.For<IIterationPlanningService>();
    private readonly FakePmSettingsStorage _settingsStorage = new()
    {
        StoredJson = """{"planningBoardNodeId":"PVT_board","capacity":8,"stallDays":3,"neglectDays":14,"excludedRepositories":[]}""",
    };

    [Fact]
    public async Task PmWorkflowPlanning_RouteShell_ExposesPageTestIdAndHeading()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/pm-workflow/planning");
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-page\"", cut.Markup);
            Assert.Contains("Iteration Planning", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-shell\"", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-no-board\"", cut.Markup);
            Assert.Contains("Select a planning board", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenBoardIsSelected_ShowsUpNextAndCandidates()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [
                    new IterationPlanningUpNextItemDto(
                        "PVTI_one",
                        PmWorkItemTypeDto.Issue,
                        40,
                        "Existing Up Next",
                        "https://github.com/owner/repo-a/issues/40",
                        "owner/repo-a",
                        1,
                        ["type/story", "priority/medium"]),
                ],
                [
                    new IterationPlanningCandidateDto(
                        PmWorkItemTypeDto.Issue,
                        50,
                        "Candidate story",
                        "https://github.com/owner/repo-a/issues/50",
                        "owner/repo-a",
                        ["type/story", "priority/high"],
                        "Todo",
                        "PVTI_candidate"),
                ],
                [],
                true,
                2,
                1,
                8,
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-up-next\"", cut.Markup);
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-candidates\"", cut.Markup);
            Assert.Contains("owner/repo-a#50", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-add-button\"", cut.Markup);
            Assert.Contains(">Issue<", cut.Markup);
            Assert.Contains("pm-workflow-planning-kind-chip", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-next-focus-order\"", cut.Markup);
            Assert.Contains("Next story Focus Order: 2", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-focus-order-chip\"", cut.Markup);
            Assert.Contains("Will assign 2", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenBoardHasNoFocusOrderField_ShowsWarning()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PmWorkItemTypeDto.Issue,
                        50,
                        "Candidate story",
                        "https://github.com/owner/repo-a/issues/50",
                        "owner/repo-a",
                        ["type/story", "priority/high"],
                        "Todo",
                        "PVTI_candidate"),
                ],
                [],
                false,
                0,
                0,
                8,
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-no-focus-order-field\"", cut.Markup);
            Assert.Contains("Unavailable on this board", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenActiveLoadAtCapacity_ShowsCapacityWarning()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView());

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-capacity\"", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-active-load\"", cut.Markup);
            Assert.Contains("8 / 8", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-capacity-warning\"", cut.Markup);
            Assert.Contains("At or above your capacity limit", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenAtCapacityAndAddCancelled_DoesNotCallAddToUpNext()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowMessageBoxAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>())
            .Returns((bool?)null);

        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView());

        await using var ctx = CreateContext(dialogService);
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"pm-workflow-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-planning-add-button']").Click());

        await dialogService.Received(1).ShowMessageBoxAsync(
            "Exceed capacity limit?",
            "Active load is already at or above your capacity limit. Add this item anyway?",
            "Add anyway",
            null,
            "Cancel",
            Arg.Any<DialogOptions>());

        await _planningService.DidNotReceive().AddToUpNextAsync(
            Arg.Any<string>(),
            Arg.Any<PmWorkItemTypeDto>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenAtCapacityAndAddConfirmed_CallsAddToUpNext()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowMessageBoxAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>())
            .Returns((bool?)true);

        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView(),
            CreateAtCapacityPlanningView());
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PmWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(AddedBoardCard: false, FocusOrderAssigned: 2, FocusOrderSkipped: false));

        await using var ctx = CreateContext(dialogService);
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"pm-workflow-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-planning-add-button']").Click());

        await dialogService.Received(1).ShowMessageBoxAsync(
            "Exceed capacity limit?",
            "Active load is already at or above your capacity limit. Add this item anyway?",
            "Add anyway",
            null,
            "Cancel",
            Arg.Any<DialogOptions>());

        cut.WaitForAssertion(() =>
        {
            _planningService.Received(1).AddToUpNextAsync(
                "PVT_board",
                PmWorkItemTypeDto.Issue,
                "owner/repo-a",
                50,
                Arg.Any<IReadOnlyList<string>>(),
                3,
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenStalledUpNextItemsExist_DisablesAddToUpNext()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PmWorkItemTypeDto.Issue,
                        50,
                        "Candidate story",
                        "https://github.com/owner/repo-a/issues/50",
                        "owner/repo-a",
                        ["type/story", "priority/high"],
                        "Todo",
                        "PVTI_candidate"),
                ],
                [],
                true,
                1,
                1,
                8,
                false,
                [
                    new IterationPlanningStalledItemDto(
                        "PVTI_stalled",
                        PmWorkItemTypeDto.Issue,
                        275,
                        "Stalled story",
                        "https://github.com/owner/repo-a/issues/275",
                        "owner/repo-a",
                        4,
                        false,
                        ["type/story"]),
                ]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-stall-gate-alert\"", cut.Markup);
            Assert.Contains("Resolve stalled Up Next items before adding new work", cut.Markup);
            var addButton = cut.Find("[data-testid=\"pm-workflow-planning-add-button\"]");
            Assert.True(addButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenAddToUpNextSucceeds_ShowsSuccessSnackbar()
    {
        ConfigureDefaults();
        var initialView = CreatePlanningViewWithCandidate();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            initialView,
            initialView);
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PmWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(
                AddedBoardCard: false,
                FocusOrderAssigned: 2,
                FocusOrderSkipped: false));

        await using var ctx = CreateContext();
        var snackbarProvider = ctx.Render<MudSnackbarProvider>();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"pm-workflow-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            var snackbar = snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Added owner/repo-a#50 to Up Next with Focus Order 2.", snackbar.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenAddToUpNextFails_ShowsErrorSnackbar()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            CreatePlanningViewWithCandidate());
        _planningService.AddToUpNextAsync(
            Arg.Any<string>(),
            Arg.Any<PmWorkItemTypeDto>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IterationPlanningAddToUpNextResultDto>(
                new InvalidOperationException("Resolve stalled Up Next items before adding new work.")));

        await using var ctx = CreateContext();
        var snackbarProvider = ctx.Render<MudSnackbarProvider>();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"pm-workflow-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            var snackbar = snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Resolve stalled Up Next items before adding new work.", snackbar.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task PmWorkflowPlanning_WhenBoardHasUpNextItems_ShowsBulkMilestoneControls()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [
                    new IterationPlanningUpNextItemDto(
                        "PVTI_one",
                        PmWorkItemTypeDto.Issue,
                        40,
                        "Existing Up Next",
                        "https://github.com/owner/repo-a/issues/40",
                        "owner/repo-a",
                        1,
                        ["type/story", "priority/medium"]),
                ],
                [],
                [],
                true,
                2,
                1,
                8,
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-planning-bulk-milestone\"", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-milestone-select\"", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-milestone-apply\"", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-planning-up-next-checkbox\"", cut.Markup);
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
            new PmProjectBoardDiscoveryDto(
                [new PmPlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));
    }

    private BunitContext CreateContext(IDialogService? dialogService = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        if (dialogService is not null)
        {
            ctx.Services.AddSingleton(dialogService);
        }
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddSingleton<IPmSettingsStorage>(_settingsStorage);
        ctx.Services.AddScoped<IPmSettingsService, PmSettingsService>();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _projectBoardDiscoveryService);
        ctx.Services.AddScoped(_ => _planningService);
        ctx.Services.AddScoped<PmWorkflowChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();

        return ctx;
    }

    private static IterationPlanningViewDto CreateAtCapacityPlanningView() =>
        CreatePlanningViewWithCandidate(activeLoad: 8, capacity: 8, isAtOrOverCapacity: true);

    private static IterationPlanningViewDto CreatePlanningViewWithCandidate(
        int activeLoad = 1,
        int capacity = 8,
        bool isAtOrOverCapacity = false) =>
        new(
            [],
            [
                new IterationPlanningCandidateDto(
                    PmWorkItemTypeDto.Issue,
                    50,
                    "Candidate story",
                    "https://github.com/owner/repo-a/issues/50",
                    "owner/repo-a",
                    ["type/story", "priority/high"],
                    "Todo",
                    "PVTI_candidate"),
            ],
            [],
            true,
            2,
            activeLoad,
            capacity,
            isAtOrOverCapacity,
            []);

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class FakePmSettingsStorage : IPmSettingsStorage
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
