using Bunit;
using Microsoft.AspNetCore.Components;
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

/// <summary>Component tests for Iteration Planning on <see cref="PlanningIteration"/>.</summary>
public sealed class PlanningIterationTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPlanningProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPlanningProjectBoardDiscoveryService>();
    private readonly IIterationPlanningService _planningService = Substitute.For<IIterationPlanningService>();
    private readonly FakePlanningSettingsStorage _settingsStorage = new()
    {
        StoredJson = """{"planningBoardNodeId":"PVT_board","capacity":8,"stallDays":3,"neglectDays":14,"excludedRepositories":[]}""",
    };
    private IRenderedComponent<MudSnackbarProvider> _snackbarProvider = default!;

    [Fact]
    public async Task PlanningIteration_RouteShell_ExposesPageTestIdAndHeading()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/planning/iteration");
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-page\"", cut.Markup);
            Assert.Contains("Iteration Planning", cut.Markup);
            Assert.Contains("data-testid=\"planning-shell\"", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-no-board\"", cut.Markup);
            Assert.Contains("Select a planning board", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenBoardIsSelected_ShowsUpNextAndCandidates()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [
                    new IterationPlanningUpNextItemDto(
                        "PVTI_one",
                        PlanningWorkItemTypeDto.Issue,
                        40,
                        "Existing Up Next",
                        "https://github.com/owner/repo-a/issues/40",
                        "owner/repo-a",
                        1,
                        ["type/story", "priority/medium"]),
                ],
                [
                    new IterationPlanningCandidateDto(
                        PlanningWorkItemTypeDto.Issue,
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
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-up-next\"", cut.Markup);
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-candidates\"", cut.Markup);
            Assert.Contains("owner/repo-a#50", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup);
            Assert.Contains(">Issue<", cut.Markup);
            Assert.Contains("planning-planning-kind-chip", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-focus-order-chip\"", cut.Markup);
            Assert.Contains("Will assign 2", cut.Markup);
            Assert.DoesNotContain("data-testid=\"planning-planning-next-focus-order\"", cut.Markup);
            Assert.DoesNotContain("Next story Focus Order:", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenBoardHasNoFocusOrderField_ShowsWarning()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PlanningWorkItemTypeDto.Issue,
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
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-no-focus-order-field\"", cut.Markup);
            Assert.DoesNotContain("Unavailable on this board", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenActiveLoadAtCapacity_ShowsCapacityWarning()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView());

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-capacity\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-active-load\"", cut.Markup);
            Assert.Contains("8 / 8", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-capacity-warning\"", cut.Markup);
            Assert.Contains("At or above your capacity limit", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenAtCapacityAndAddCancelled_DoesNotCallAddToUpNext()
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
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView());

        await using var ctx = CreateContext(dialogService);
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

        await dialogService.Received(1).ShowMessageBoxAsync(
            "Exceed capacity limit?",
            "Active load is already at or above your capacity limit. Add this item anyway?",
            "Add anyway",
            null,
            "Cancel",
            Arg.Any<DialogOptions>());

        await _planningService.DidNotReceive().AddToUpNextAsync(
            Arg.Any<string>(),
            Arg.Any<PlanningWorkItemTypeDto>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningIteration_WhenAtCapacityAndAddConfirmed_CallsAddToUpNext()
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
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView(),
            CreateAtCapacityPlanningView());
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PlanningWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(AddedBoardCard: false, ProjectItemId: "PVTI_candidate", FocusOrderAssigned: 2, FocusOrderSkipped: false));

        await using var ctx = CreateContext(dialogService);
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

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
                PlanningWorkItemTypeDto.Issue,
                "owner/repo-a",
                50,
                Arg.Any<IReadOnlyList<string>>(),
                3,
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenStalledUpNextItemsExist_DisablesAddToUpNext()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PlanningWorkItemTypeDto.Issue,
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
                        PlanningWorkItemTypeDto.Issue,
                        275,
                        "Stalled story",
                        "https://github.com/owner/repo-a/issues/275",
                        "owner/repo-a",
                        4,
                        false,
                        ["type/story"]),
                ]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-stall-gate-alert\"", cut.Markup);
            Assert.Contains("mud-alert-outlined-error", cut.Markup);
            Assert.Contains("You cannot add to Up Next until stalled items are handled", cut.Markup);
            Assert.Contains("1 item is stalled", cut.Markup);
            Assert.Contains("Re-commit", cut.Markup);
            Assert.Contains("Mark Blocked", cut.Markup);
            Assert.Contains("Ice Box", cut.Markup);
            Assert.Contains("Remove", cut.Markup);
            Assert.DoesNotContain("capacity", cut.Find("[data-testid=\"planning-planning-stall-gate-alert\"]").TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-testid=\"planning-planning-candidate-pause-line\"", cut.Markup);
            Assert.Contains("Add is paused until stalled Up Next is cleared.", cut.Markup);
            var addButton = cut.Find("[data-testid=\"planning-planning-add-button\"]");
            Assert.True(addButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenStalledAndAtCapacity_DisablesAddOnlyForStall()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PlanningWorkItemTypeDto.Issue,
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
                8,
                8,
                true,
                [
                    new IterationPlanningStalledItemDto(
                        "PVTI_stalled",
                        PlanningWorkItemTypeDto.Issue,
                        275,
                        "Stalled story",
                        "https://github.com/owner/repo-a/issues/275",
                        "owner/repo-a",
                        4,
                        false,
                        ["type/story"]),
                ]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-capacity\"", cut.Markup);
            Assert.Contains("8 / 8", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-stall-gate-alert\"", cut.Markup);
            Assert.DoesNotContain("data-testid=\"planning-planning-up-next-capacity-status\"", cut.Markup);
            var addButton = cut.Find("[data-testid=\"planning-planning-add-button\"]");
            Assert.True(addButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenAtCapacityWithoutStall_ShowsSoftCapacityStatusInUpNext()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreateAtCapacityPlanningView());

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-up-next-capacity-status\"", cut.Markup);
            Assert.Contains("You can still add items after confirming.", cut.Markup);
            var addButton = cut.Find("[data-testid=\"planning-planning-add-button\"]");
            Assert.False(addButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenStalledItemRemoved_UpdatesLocallyWithoutFullReload()
    {
        ConfigureDefaults();
        var stalledItem = new IterationPlanningStalledItemDto(
            "PVTI_stalled",
            PlanningWorkItemTypeDto.Issue,
            275,
            "Stalled story",
            "https://github.com/owner/repo-a/issues/275",
            "owner/repo-a",
            4,
            false,
            ["type/story"]);
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [],
                [
                    new IterationPlanningCandidateDto(
                        PlanningWorkItemTypeDto.Issue,
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
                [stalledItem]));
        _planningService
            .RemoveStalledUpNextItemAsync("PVT_board", stalledItem, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-stall-gate-alert\"", cut.Markup);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-remove-button']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("data-testid=\"planning-planning-stall-gate-alert\"", cut.Markup);
            Assert.DoesNotContain("data-testid=\"planning-planning-stalled\"", cut.Markup);
            Assert.DoesNotContain("data-testid=\"planning-planning-candidate-pause-line\"", cut.Markup);
            var addButton = cut.Find("[data-testid=\"planning-planning-add-button\"]");
            Assert.False(addButton.HasAttribute("disabled"));
        });

        await _planningService.Received(1).GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _planningService.Received(1)
            .RemoveStalledUpNextItemAsync("PVT_board", stalledItem, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningIteration_WhenRefreshClicked_ReloadsPlanningView()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreatePlanningViewWithCandidate());

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-refresh\"", cut.Markup);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-refresh']").Click());

        cut.WaitForAssertion(() =>
        {
            _planningService.Received(1).GetPlanningViewAsync("PVT_board", 8, 3, false, Arg.Any<CancellationToken>());
            _planningService.Received(1).GetPlanningViewAsync("PVT_board", 8, 3, true, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenAddToUpNextSucceeds_UpdatesLocallyWithoutFullReload()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreatePlanningViewWithCandidate());
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PlanningWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(
                AddedBoardCard: false,
                ProjectItemId: "PVTI_candidate",
                FocusOrderAssigned: 2,
                FocusOrderSkipped: false));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-up-next-table\"", cut.Markup);
            Assert.Contains("owner/repo-a#50", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-focus-order-chip\"", cut.Markup);
            Assert.Contains(">2<", cut.Markup);
            Assert.Contains("2 / 8", cut.Markup);
        });

        await _planningService.Received(1).GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningIteration_WhenAddToUpNextSucceeds_InvalidatesDailyFocusAndBacklogCaches()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreatePlanningViewWithCandidate());
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PlanningWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(
                AddedBoardCard: false,
                ProjectItemId: "PVTI_candidate",
                FocusOrderAssigned: 2,
                FocusOrderSkipped: false));

        await using var ctx = CreateContext();
        var coordinator = ctx.Services.GetRequiredService<PlanningChromeCoordinator>();
        coordinator.SetDailyFocusBoardState(
            "PVT_board",
            8,
            3,
            new DailyFocusBoardStateDto([], 1, 8, 8, [], 3),
            null,
            isLoading: false);
        coordinator.SetBacklogReview("PVT_board", new BacklogReviewResultDto([], [], [], [], [], [], false, []), null, isLoading: false);

        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Null(coordinator.DailyFocusBoardState);
            Assert.Null(coordinator.BacklogReview);
            Assert.NotNull(coordinator.IterationPlanning?.View);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenAddToUpNextSucceeds_ShowsSuccessSnackbar()
    {
        ConfigureDefaults();
        var initialView = CreatePlanningViewWithCandidate();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            initialView);
        _planningService.AddToUpNextAsync(
            "PVT_board",
            PlanningWorkItemTypeDto.Issue,
            "owner/repo-a",
            50,
            Arg.Any<IReadOnlyList<string>>(),
            3,
            Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningAddToUpNextResultDto(
                AddedBoardCard: false,
                ProjectItemId: "PVTI_candidate",
                FocusOrderAssigned: 2,
                FocusOrderSkipped: false));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            var snackbar = _snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Added owner/repo-a#50 to Up Next with Focus Order 2.", snackbar.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenAddToUpNextFails_ShowsErrorSnackbar()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            CreatePlanningViewWithCandidate());
        _planningService.AddToUpNextAsync(
            Arg.Any<string>(),
            Arg.Any<PlanningWorkItemTypeDto>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IterationPlanningAddToUpNextResultDto>(
                new InvalidOperationException("Resolve stalled Up Next items before adding new work.")));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"planning-planning-add-button\"", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-planning-add-button']").Click());

        cut.WaitForAssertion(() =>
        {
            var snackbar = _snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Resolve stalled Up Next items before adding new work.", snackbar.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task PlanningIteration_WhenBoardHasUpNextItems_ShowsBulkMilestoneControls()
    {
        ConfigureDefaults();
        _planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto(
                [
                    new IterationPlanningUpNextItemDto(
                        "PVTI_one",
                        PlanningWorkItemTypeDto.Issue,
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
        var cut = ctx.RenderPlanningPage<PlanningIteration>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-planning-bulk-milestone\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-milestone-select\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-milestone-apply\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-planning-up-next-checkbox\"", cut.Markup);
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
        ctx.Services.AddSingleton<IPlanningSettingsStorage>(_settingsStorage);
        ctx.Services.AddScoped<IPlanningSettingsService, PlanningSettingsService>();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _projectBoardDiscoveryService);
        ctx.Services.AddScoped(_ => _planningService);
        ctx.Services.AddScoped(_ => PlanningTestChromeDependencies.CreateBoardCompatibilityService());
        ctx.Services.AddScoped<PlanningChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        _snackbarProvider = ctx.Render<MudSnackbarProvider>();

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
                    PlanningWorkItemTypeDto.Issue,
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
