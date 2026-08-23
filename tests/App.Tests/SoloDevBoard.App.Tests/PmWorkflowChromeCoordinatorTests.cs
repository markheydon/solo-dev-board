using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.PmWorkflow;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Unit tests for <see cref="PmWorkflowChromeCoordinator"/> load caching and cancellation.</summary>
public sealed class PmWorkflowChromeCoordinatorTests
{
    private readonly IPmSettingsService _pmSettingsService = Substitute.For<IPmSettingsService>();
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService =
        Substitute.For<IPmProjectBoardDiscoveryService>();

    [Fact]
    public async Task EnsureLoadedAsync_WhenAlreadyLoaded_DoesNotCallRepositoryServiceAgain()
    {
        ConfigureSuccessfulLoad();

        var coordinator = CreateCoordinator();
        await coordinator.EnsureLoadedAsync();
        await coordinator.EnsureLoadedAsync();

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WhenCalledTwiceWhileInFlight_UsesSingleRepositoryFetch()
    {
        ConfigureSuccessfulLoad(delayRepositoryFetch: true);

        var coordinator = CreateCoordinator();
        var firstRefresh = coordinator.RefreshAsync();
        var secondRefresh = coordinator.RefreshAsync();

        await Task.WhenAll(firstRefresh, secondRefresh);

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelPendingLoad_WhenLoadIsInFlight_StopsFurtherRepositoryUpdates()
    {
        var repositoryTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _pmSettingsService.GetSettingsAsync()
            .Returns(PmSettingsDefaults.Create());
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(repositoryTask.Task);

        var coordinator = CreateCoordinator();
        var refreshTask = coordinator.RefreshAsync();
        coordinator.CancelPendingLoad();
        repositoryTask.SetResult([CreateRepository("owner", "repo-a")]);

        await refreshTask;

        await _projectBoardDiscoveryService.DidNotReceive()
            .GetPlanningBoardOptionsForRepositoriesAsync(
                Arg.Any<IReadOnlyList<RepositoryDto>>(),
                Arg.Any<CancellationToken>());
        Assert.False(coordinator.HasLoadedOnce);
    }

    [Fact]
    public async Task RefreshAsync_WithForceReload_ClearsDailyFocusBoardStateCache()
    {
        ConfigureSuccessfulLoad();

        var coordinator = CreateCoordinator();
        await coordinator.EnsureLoadedAsync();
        coordinator.SetDailyFocusBoardState(
            "PVT_board",
            8,
            3,
            new DailyFocusBoardStateDto([], 0, 8, 0, [], 3),
            null,
            isLoading: false);

        coordinator.SetDailyFocusRecommendations("PVT_board", [], null, isLoading: false);
        coordinator.SetDailyFocusStalledReviews(
            "PVT_board",
            3,
            [],
            new DailyFocusStalledReviewSnapshotDto([], UsedInReviewColumn: false),
            null,
            isLoading: false);
        coordinator.SetBacklogReview("PVT_board", EmptyBacklogResult(), null, isLoading: false);
        coordinator.SetIterationPlanning(
            "PVT_board",
            new IterationPlanningViewDto([], [], [], true, 1, 0, 8, false, []),
            null,
            isLoading: false);

        await coordinator.RefreshAsync(forceReload: true);

        Assert.Null(coordinator.DailyFocusBoardState);
        Assert.Null(coordinator.DailyFocusRecommendations);
        Assert.Null(coordinator.DailyFocusStalledReviews);
        Assert.Null(coordinator.BacklogReview);
        Assert.Null(coordinator.IterationPlanning);
    }

    [Fact]
    public async Task SaveSettingsAsync_ClearsDailyFocusRecommendationsCache()
    {
        ConfigureSuccessfulLoad();

        var coordinator = CreateCoordinator();
        await coordinator.EnsureLoadedAsync();
        coordinator.SetDailyFocusRecommendations("PVT_board", [], null, isLoading: false);
        coordinator.SetDailyFocusStalledReviews(
            "PVT_board",
            3,
            [],
            new DailyFocusStalledReviewSnapshotDto([], UsedInReviewColumn: false),
            null,
            isLoading: false);
        coordinator.SetBacklogReview("PVT_board", EmptyBacklogResult(), null, isLoading: false);

        await coordinator.SaveSettingsAsync(PmSettingsDefaults.Create());

        Assert.Null(coordinator.DailyFocusRecommendations);
        Assert.Null(coordinator.DailyFocusStalledReviews);
        Assert.Null(coordinator.BacklogReview);
    }

    [Fact]
    public async Task EnsureLoadedAsync_WhenRepositoryFetchFails_DoesNotMarkLoaded()
    {
        _pmSettingsService.GetSettingsAsync()
            .Returns(PmSettingsDefaults.Create());
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        var coordinator = CreateCoordinator();
        await coordinator.EnsureLoadedAsync();

        Assert.False(coordinator.HasLoadedOnce);
        Assert.NotNull(coordinator.State.LoadErrorMessage);
    }

    private void ConfigureSuccessfulLoad(bool delayRepositoryFetch = false)
    {
        _pmSettingsService.GetSettingsAsync()
            .Returns(PmSettingsDefaults.Create());

        if (delayRepositoryFetch)
        {
            var repositoryTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
            _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
                .Returns(repositoryTask.Task);
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                repositoryTask.SetResult([CreateRepository("owner", "repo-a")]);
            });
        }
        else
        {
            _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
            ]);
        }

        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
                Arg.Any<IReadOnlyList<RepositoryDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PmProjectBoardDiscoveryDto([], 0, 0));
    }

    private PmWorkflowChromeCoordinator CreateCoordinator() =>
        new(
            _pmSettingsService,
            _repositoryService,
            _projectBoardDiscoveryService,
            NullLogger<PmWorkflowChromeCoordinator>.Instance);

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false);

    private static BacklogReviewResultDto EmptyBacklogResult()
        => new([], [], [], [], [], [], false, []);
}
