using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Daily Focus occupancy and stalled-review panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
public partial class PmWorkflowDailyFocusPanel : ComponentBase
{
    [CascadingParameter]
    public PmWorkflowChromeState? ChromeState { get; set; }

    [CascadingParameter(Name = "PmWorkflowDataRevision")]
    public int DataRevision { get; set; }

    [Inject]
    public PmWorkflowChromeCoordinator ChromeCoordinator { get; set; } = default!;

    [Inject]
    public IDailyFocusBoardStateService BoardStateService { get; set; } = default!;

    [Inject]
    public IDailyFocusStalledReviewService StalledReviewService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowDailyFocusPanel> Logger { get; set; } = default!;

    private DailyFocusBoardStateDto? boardState;
    private bool isLoadingBoardState;
    private string? loadErrorMessage;
    private DailyFocusStalledReviewSnapshotDto? stalledReviews;
    private bool isLoadingStalledReviews;
    private string? stalledReviewsErrorMessage;
    private int stallDaysThreshold = PmSettingsDefaults.StallDays;
    private int stalledReviewsLoadGeneration;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (ChromeState is null || ChromeState.IsLoading)
        {
            return;
        }

        stallDaysThreshold = ChromeState.Settings.StallDays > 0
            ? ChromeState.Settings.StallDays
            : PmSettingsDefaults.StallDays;

        if (!ChromeState.HasPlanningBoardSelected)
        {
            boardState = null;
            loadErrorMessage = null;
            stalledReviews = null;
            stalledReviewsErrorMessage = null;
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        var capacity = ChromeState.Settings.Capacity;
        var occupancyTask = TryApplyCachedBoardState(boardId, capacity)
            ? Task.CompletedTask
            : LoadBoardStateAsync(boardId, capacity);
        var stalledTask = TryApplyCachedStalledReviews(boardId, stallDaysThreshold, ChromeState.Settings.ExcludedRepositories)
            ? Task.CompletedTask
            : LoadStalledReviewsAsync(
                boardId,
                stallDaysThreshold,
                ChromeState.Settings.ExcludedRepositories);

        await Task.WhenAll(occupancyTask, stalledTask).ConfigureAwait(false);
    }

    private bool TryApplyCachedBoardState(string boardId, int capacity)
    {
        var cached = ChromeCoordinator.DailyFocusBoardState;
        if (cached is null
            || !cached.BoardId.Equals(boardId, StringComparison.Ordinal)
            || cached.Capacity != capacity)
        {
            return false;
        }

        isLoadingBoardState = cached.IsLoading;
        boardState = cached.State;
        loadErrorMessage = cached.ErrorMessage;
        return cached.IsLoading || cached.State is not null || !string.IsNullOrWhiteSpace(cached.ErrorMessage);
    }

    private bool TryApplyCachedStalledReviews(
        string boardId,
        int stallDays,
        IReadOnlyList<string> excludedRepositories)
    {
        var cached = ChromeCoordinator.DailyFocusStalledReviews;
        if (cached is null
            || !cached.BoardId.Equals(boardId, StringComparison.Ordinal)
            || cached.StallDays != stallDays
            || !ExcludedRepositoriesMatch(cached.ExcludedRepositories, excludedRepositories))
        {
            return false;
        }

        isLoadingStalledReviews = cached.IsLoading;
        stalledReviews = cached.Snapshot;
        stalledReviewsErrorMessage = cached.ErrorMessage;
        return cached.IsLoading || cached.Snapshot is not null || !string.IsNullOrWhiteSpace(cached.ErrorMessage);
    }

    private static bool ExcludedRepositoriesMatch(
        IReadOnlyList<string> cached,
        IReadOnlyList<string> current)
        => cached.Count == current.Count
            && cached.SequenceEqual(current, StringComparer.OrdinalIgnoreCase);

    private Task RetryLoadBoardStateAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearDailyFocusBoardState();
        return LoadBoardStateAsync(ChromeState.Settings.PlanningBoardNodeId, ChromeState.Settings.Capacity);
    }

    private Task RetryLoadStalledReviewsAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        stallDaysThreshold = ChromeState.Settings.StallDays > 0
            ? ChromeState.Settings.StallDays
            : PmSettingsDefaults.StallDays;

        ChromeCoordinator.ClearDailyFocusStalledReviews();
        return LoadStalledReviewsAsync(
            ChromeState.Settings.PlanningBoardNodeId,
            stallDaysThreshold,
            ChromeState.Settings.ExcludedRepositories);
    }

    private async Task LoadBoardStateAsync(string boardId, int capacity)
    {
        if (TryApplyCachedBoardState(boardId, capacity))
        {
            return;
        }

        isLoadingBoardState = true;
        loadErrorMessage = null;
        boardState = null;
        ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, null, null, isLoading: true);

        try
        {
            boardState = await BoardStateService.GetBoardStateAsync(boardId, capacity).ConfigureAwait(false);
            ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, boardState, null, isLoading: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus board occupancy.");
            boardState = null;
            loadErrorMessage = "Unable to load board occupancy. Check your GitHub connection and try again.";
            ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, null, loadErrorMessage, isLoading: false);
        }
        finally
        {
            isLoadingBoardState = false;
        }
    }

    private async Task LoadStalledReviewsAsync(
        string boardId,
        int stallDays,
        IReadOnlyList<string> excludedRepositories)
    {
        if (TryApplyCachedStalledReviews(boardId, stallDays, excludedRepositories))
        {
            return;
        }

        var generation = Interlocked.Increment(ref stalledReviewsLoadGeneration);
        isLoadingStalledReviews = true;
        stalledReviewsErrorMessage = null;
        stalledReviews = null;
        ChromeCoordinator.SetDailyFocusStalledReviews(
            boardId,
            stallDays,
            excludedRepositories,
            null,
            null,
            isLoading: true);

        try
        {
            var snapshot = await StalledReviewService
                .GetStalledReviewPullRequestsAsync(boardId, stallDays, excludedRepositories)
                .ConfigureAwait(false);

            if (!ShouldApplyStalledReviewsResult(generation, boardId))
            {
                return;
            }

            stalledReviews = snapshot;
            ChromeCoordinator.SetDailyFocusStalledReviews(
                boardId,
                stallDays,
                excludedRepositories,
                snapshot,
                null,
                isLoading: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus stalled review pull requests.");

            if (!ShouldApplyStalledReviewsResult(generation, boardId))
            {
                return;
            }

            stalledReviews = null;
            stalledReviewsErrorMessage =
                "Unable to load pull requests awaiting review. Check your GitHub connection and try again.";
            ChromeCoordinator.SetDailyFocusStalledReviews(
                boardId,
                stallDays,
                excludedRepositories,
                null,
                stalledReviewsErrorMessage,
                isLoading: false);
        }
        finally
        {
            if (generation == stalledReviewsLoadGeneration)
            {
                isLoadingStalledReviews = false;
            }
        }
    }

    private bool ShouldApplyStalledReviewsResult(int generation, string boardId)
    {
        if (generation != stalledReviewsLoadGeneration)
        {
            return false;
        }

        var currentBoardId = ChromeState?.Settings.PlanningBoardNodeId;
        return !string.IsNullOrWhiteSpace(currentBoardId)
            && currentBoardId.Equals(boardId, StringComparison.Ordinal);
    }
}
