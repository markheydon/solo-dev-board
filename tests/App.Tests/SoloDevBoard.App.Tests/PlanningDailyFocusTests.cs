using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.Planning;
using SoloDevBoard.App.Components.Features.Planning.Pages;
using SoloDevBoard.App.Components.Shell.Layout;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Daily Focus occupancy on <see cref="PlanningDailyFocus"/>.</summary>
public sealed class PlanningDailyFocusTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPlanningProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPlanningProjectBoardDiscoveryService>();
    private readonly IDailyFocusBoardStateService _boardStateService = Substitute.For<IDailyFocusBoardStateService>();
    private readonly IDailyFocusStalledReviewService _stalledReviewService = Substitute.For<IDailyFocusStalledReviewService>();
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService = Substitute.For<IPlanningWorkItemCatalogueService>();
    private readonly IDailyFocusRecommendationService _recommendationService = Substitute.For<IDailyFocusRecommendationService>();
    private readonly FakePlanningSettingsStorage _settingsStorage = new();

    public PlanningDailyFocusTests()
    {
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new DailyFocusStalledReviewSnapshotDto([], UsedInReviewColumn: false));
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(new PlanningWorkItemCatalogueResultDto([], [], []));
    }

    [Fact]
    public void PlanningLayout_UsesMainLayoutAsParent()
    {
        var layoutAttribute = typeof(PlanningLayout)
            .GetCustomAttributes(typeof(LayoutAttribute), inherit: true)
            .Cast<LayoutAttribute>()
            .FirstOrDefault();

        Assert.NotNull(layoutAttribute);
        Assert.Equal(typeof(MainLayout), layoutAttribute.LayoutType);
    }

    [Fact]
    public async Task PlanningDailyFocus_RouteShell_ExposesPageTestIdAndHeading()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/planning/daily-focus");
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"planning-daily-focus-page\"", cut.Markup);
            Assert.Contains("Daily Focus", cut.Markup);
            Assert.Contains("data-testid=\"planning-shell\"", cut.Markup);
            Assert.Contains("data-testid=\"planning-tab-strip\"", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Daily Focus", cut.Markup);
            Assert.Contains("Select a planning board in the dropdown above to load Daily Focus occupancy and recommendations.", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenBoardHasItems_ShowsOccupancyAndActiveLoad()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [
                    new DailyFocusOccupancyChipDto("Todo", 2),
                    new DailyFocusOccupancyChipDto("Up Next", 4),
                    new DailyFocusOccupancyChipDto("In Progress", 2),
                ],
                ActiveLoad: 6,
                Capacity: 8,
                ItemCount: 8,
                StalledUpNextItems: [],
                StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Todo 2", cut.Markup);
            Assert.Contains("Up Next 4", cut.Markup);
            Assert.Contains("In Progress 2", cut.Markup);
            Assert.Contains("Active load: 6 / 8 (Up Next + In Progress)", cut.Markup);
            Assert.Contains("No Up Next items have been stalled for 3 or more days.", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenUpNextItemsAreStalled_ShowsTitleAgeAndGitHubLink()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Up Next", 1)],
                ActiveLoad: 1,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems:
                [
                    new DailyFocusStalledItemDto(
                        "Stalled story",
                        4,
                        "https://github.com/owner/repo/issues/42",
                        UsedUpdatedAtFallback: false),
                ],
                StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Stalled story", cut.Markup);
            Assert.Contains("(4d)", cut.Markup);
            Assert.Contains("https://github.com/owner/repo/issues/42", cut.Markup);
            Assert.Contains("Open Stalled story on GitHub", cut.Markup);
            Assert.Contains("noopener", cut.Markup);
            Assert.DoesNotContain("Status-changed-at was not available", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenStallUsesUpdatedAtFallback_ShowsFootnote()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Up Next", 1)],
                ActiveLoad: 1,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems:
                [
                    new DailyFocusStalledItemDto(
                        "Fallback stall",
                        5,
                        "https://github.com/owner/repo/issues/7",
                        UsedUpdatedAtFallback: true),
                ],
                StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Fallback stall", cut.Markup);
            Assert.Contains("Age uses the item last-updated time because Status-changed-at was not available.", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenBoardHasNoItems_ShowsEmptyAlert()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 0)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 0,
                StalledUpNextItems: [],
                StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This planning board has no items.", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenCatalogueFails_ShowsErrorWithRetry()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load board occupancy.", cut.Markup);
            Assert.Contains("Retry", cut.Markup);
        });

        cut.Render();

        await _boardStateService.Received(1).GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenRetryClickedAfterCatalogueFailure_ReloadsBoardState()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => new DailyFocusBoardStateDto(
                    [new DailyFocusOccupancyChipDto("Todo", 1)],
                    ActiveLoad: 0,
                    Capacity: 8,
                    ItemCount: 1,
                    StalledUpNextItems: [],
                    StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Unable to load board occupancy.", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-daily-focus-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Todo 1", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
        });

        await _boardStateService.Received(2).GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenStalledReviewPullRequestsExist_ShowsRepoNumberAgeAndLink()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("In Review", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                3));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new DailyFocusStalledReviewSnapshotDto(
                [
                    new DailyFocusStalledReviewPullRequestDto(
                        "owner/repo",
                        12,
                        5,
                        "https://github.com/owner/repo/pull/12",
                        "Stalled review"),
                ],
                UsedInReviewColumn: true));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("PRs awaiting review 3+ days", cut.Markup);
            Assert.Contains("owner/repo#12", cut.Markup);
            Assert.Contains("(5d)", cut.Markup);
            Assert.Contains("https://github.com/owner/repo/pull/12", cut.Markup);
            Assert.Contains(">Open<", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenNoStalledReviewPullRequests_ShowsEmptyAlert()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No pull requests have been awaiting review for 3 or more days.", cut.Markup);
            Assert.Contains("data-testid=\"planning-daily-focus-stalled-reviews-empty\"", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenStalledReviewLoadFails_ShowsErrorWithRetry()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                3));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load pull requests awaiting review.", cut.Markup);
            Assert.Contains("data-testid=\"planning-daily-focus-stalled-reviews-retry\"", cut.Markup);
        });

        await _stalledReviewService.Received(1).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenRetryClickedAfterStalledReviewFailure_ReloadsStalledReviews()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                3));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => new DailyFocusStalledReviewSnapshotDto(
                    [
                        new DailyFocusStalledReviewPullRequestDto(
                            "owner/repo",
                            12,
                            5,
                            "https://github.com/owner/repo/pull/12",
                            "Stalled review"),
                    ],
                    UsedInReviewColumn: true));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Unable to load pull requests awaiting review.", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-daily-focus-stalled-reviews-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo#12", cut.Markup);
            Assert.Contains("(5d)", cut.Markup);
        });

        await _stalledReviewService.Received(2).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenStallDaysIsConfigured_IntroUsesThreshold()
    {
        _settingsStorage.StoredJson = """{"capacity":8,"stallDays":5,"neglectDays":14,"excludedRepositories":[]}""";
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 5, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                5));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("awaiting review for 5 or more days", cut.Markup);
            Assert.Contains("PRs awaiting review 5+ days", cut.Markup);
            Assert.DoesNotContain("three or more days", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenReturningToDailyFocus_DoesNotReloadStalledReviews()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                [],
                3));

        await using var ctx = CreateContext();
        var dailyFocus = ctx.RenderPlanningPage<PlanningDailyFocus>();

        dailyFocus.WaitForAssertion(() =>
            Assert.Contains("No pull requests have been awaiting review for 3 or more days.", dailyFocus.Markup));

        ctx.RenderPlanningPage<PlanningRepos>();
        ctx.RenderPlanningPage<PlanningDailyFocus>();

        await _stalledReviewService.Received(1).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenLinkedBoardsAreInaccessible_ShowsWarning()
    {
        ConfigureDefaults();
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto(
                [new PlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                2,
                1));
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto([], 0, 8, 0, [], 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("could not be loaded", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenChromeLoadIsInFlight_ShowsLoadingIndicator()
    {
        var repositoriesReady = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(repositoriesReady.Task);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Loading Planning", cut.Markup);
            Assert.Contains("Loading planning boards, occupancy, and recommendations.", cut.Markup);
        });

        repositoriesReady.SetResult([CreateRepository("owner", "repo-a")]);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select a planning board in the dropdown above to load Daily Focus occupancy and recommendations.", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningLayout_DisposeWhileStillOnPmWorkflow_DoesNotCancelChromeLoad()
    {
        var repositoriesReady = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(repositoriesReady.Task);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto(
                [new PlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));

        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/planning/daily-focus");
        var coordinator = ctx.Services.GetRequiredService<PlanningChromeCoordinator>();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Loading Planning", cut.Markup));

        cut.Instance.Dispose();
        repositoriesReady.SetResult([CreateRepository("owner", "repo-a")]);

        cut.WaitForAssertion(() => Assert.True(coordinator.HasLoadedOnce));
    }

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenChromeAlreadyLoaded_DoesNotFetchRepositoriesAgain()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));

        await using var ctx = CreateContext();
        var dailyFocus = ctx.RenderPlanningPage<PlanningDailyFocus>();

        dailyFocus.WaitForAssertion(() => Assert.Contains("Active load: 0 / 8", dailyFocus.Markup));

        ctx.RenderPlanningPage<PlanningRepos>();

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenOccupancyReturnsFirst_ShowsOccupancyBeforeRecommendations()
    {
        ConfigureDefaults();
        var occupancyReady = new TaskCompletionSource<DailyFocusBoardStateDto>();
        var recommendationsReady = new TaskCompletionSource<DailyFocusRecommendationResultDto>();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(_ => occupancyReady.Task);
        _recommendationService.GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(_ => recommendationsReady.Task);

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        occupancyReady.SetResult(new DailyFocusBoardStateDto(
            [new DailyFocusOccupancyChipDto("Todo", 1)],
            ActiveLoad: 0,
            Capacity: 8,
            ItemCount: 1,
            StalledUpNextItems: [],
            StallDays: 3));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Todo 1", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
            Assert.DoesNotContain("Recommended today (all included repositories)", cut.Markup);
            Assert.DoesNotContain("No unblocked work items to recommend today.", cut.Markup);
            Assert.Contains("Loading Daily Focus recommendations", cut.Markup);
        });

        recommendationsReady.SetResult(CreateRecommendationResult(
            new DailyFocusRecommendationDto(
                1,
                "owner/repo-a",
                40,
                "Ship Daily Focus",
                "https://github.com/owner/repo-a/issues/40",
                "priority/high")));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Recommended today (all included repositories)", cut.Markup);
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("Todo 1", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenRecommendationsExist_ShowsRankedRowsWithGitHubLinks()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));
        _recommendationService.GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(CreateRecommendationResult(
                new DailyFocusRecommendationDto(
                    1,
                    "owner/repo-a",
                    40,
                    "Ship Daily Focus",
                    "https://github.com/owner/repo-a/issues/40",
                    "priority/high"),
                new DailyFocusRecommendationDto(
                    2,
                    "owner/repo-a",
                    41,
                    "Unlabelled follow-up",
                    "https://github.com/owner/repo-a/issues/41",
                    null)));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Recommended today (all included repositories)", cut.Markup);
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("priority/high", cut.Markup);
            Assert.Contains("Unlabelled", cut.Markup);
            Assert.Contains("https://github.com/owner/repo-a/issues/40", cut.Markup);
            Assert.Contains("Ship Daily Focus", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenNoRecommendations_ShowsEmptyAlert()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No unblocked work items to recommend today.", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenRecommendationsFail_ShowsErrorWithRetryAndKeepsOccupancy()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));
        _recommendationService.GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => CreateRecommendationResult(
                    new DailyFocusRecommendationDto(
                        1,
                        "owner/repo-a",
                        42,
                        "Recovered item",
                        "https://github.com/owner/repo-a/issues/42",
                        "priority/medium")));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load recommended work.", cut.Markup);
            Assert.Contains("Todo 1", cut.Markup);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='planning-daily-focus-recommendations-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-a#42", cut.Markup);
            Assert.Contains("priority/medium", cut.Markup);
        });

        await _recommendationService.Received(2).GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningDailyFocus_WhenCatalogueHasPartialFailures_ShowsWarningAndRankedRows()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, 3, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1,
                StalledUpNextItems: [],
                StallDays: 3));
        _recommendationService.GetRecommendationsAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new DailyFocusRecommendationResultDto(
                [
                    new DailyFocusRecommendationDto(
                        1,
                        "owner/repo-a",
                        40,
                        "Ship Daily Focus",
                        "https://github.com/owner/repo-a/issues/40",
                        "priority/high"),
                ],
                [new PlanningRepositoryCatalogueFailureDto("markheydon/markheydon", "Not found", 404)]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Recommended today (all included repositories)", cut.Markup);
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("markheydon/markheydon", cut.Markup);
            Assert.Contains("ranked without 1 repository that failed to load", cut.Markup);
            Assert.DoesNotContain("Unable to load recommended work.", cut.Markup);
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
        _recommendationService.GetRecommendationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateRecommendationResult());
    }

    private BunitContext CreateContext()
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
        ctx.Services.AddScoped(_ => _workItemCatalogueService);
        ctx.Services.AddScoped(_ => _recommendationService);
        ctx.Services.AddScoped<PlanningChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static DailyFocusRecommendationResultDto CreateRecommendationResult(
        params DailyFocusRecommendationDto[] recommendations)
        => new(recommendations, []);

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
