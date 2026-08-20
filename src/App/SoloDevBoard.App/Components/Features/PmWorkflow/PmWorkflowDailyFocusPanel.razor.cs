using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Daily Focus occupancy, recommendations, and stalled-review panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
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
    public IDailyFocusRecommendationService RecommendationService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowDailyFocusPanel> Logger { get; set; } = default!;

    private DailyFocusBoardStateDto? boardState;
    private IReadOnlyList<DailyFocusRecommendationDto>? recommendations;
    private bool isLoadingBoardState;
    private bool isLoadingRecommendations;
    private string? loadErrorMessage;
    private string? recommendationsErrorMessage;
    private string? recommendationsWarningMessage;
    private DailyFocusStalledReviewSnapshotDto? stalledReviews;
    private bool isLoadingStalledReviews;
    private string? stalledReviewsErrorMessage;
    private int stallDaysThreshold = PmSettingsDefaults.StallDays;
    private int stalledReviewsLoadGeneration;

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        if (ChromeState is null || ChromeState.IsLoading)
        {
            return Task.CompletedTask;
        }

        stallDaysThreshold = ChromeState.Settings.StallDays > 0
            ? ChromeState.Settings.StallDays
            : PmSettingsDefaults.StallDays;

        if (!ChromeState.HasPlanningBoardSelected)
        {
            boardState = null;
            recommendations = null;
            loadErrorMessage = null;
            recommendationsErrorMessage = null;
            recommendationsWarningMessage = null;
            stalledReviews = null;
            stalledReviewsErrorMessage = null;
            return Task.CompletedTask;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        var capacity = ChromeState.Settings.Capacity;
        var stallDays = ChromeState.Settings.StallDays;
        var boardCached = TryApplyCachedBoardState(boardId, capacity, stallDays);
        var recommendationsCached = TryApplyCachedRecommendations(boardId);
        var stalledCached = TryApplyCachedStalledReviews(
            boardId,
            stallDaysThreshold,
            ChromeState.Settings.ExcludedRepositories);

        if (!boardCached)
        {
            _ = LoadBoardStateAsync(boardId, capacity, stallDays);
        }

        if (!recommendationsCached)
        {
            _ = LoadRecommendationsAsync(boardId);
        }

        if (!stalledCached)
        {
            _ = LoadStalledReviewsAsync(boardId, stallDaysThreshold, ChromeState.Settings.ExcludedRepositories);
        }

        return Task.CompletedTask;
    }

    private bool TryApplyCachedBoardState(string boardId, int capacity, int stallDays)
    {
        var cached = ChromeCoordinator.DailyFocusBoardState;
        if (cached is null
            || !cached.BoardId.Equals(boardId, StringComparison.Ordinal)
            || cached.Capacity != capacity
            || cached.StallDays != stallDays)
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

    private bool TryApplyCachedRecommendations(string boardId)
    {
        var cached = ChromeCoordinator.DailyFocusRecommendations;
        if (cached is null || !cached.BoardId.Equals(boardId, StringComparison.Ordinal))
        {
            return false;
        }

        isLoadingRecommendations = cached.IsLoading;
        recommendations = cached.Recommendations;
        recommendationsErrorMessage = cached.ErrorMessage;
        recommendationsWarningMessage = cached.WarningMessage;
        return cached.IsLoading
            || cached.Recommendations is not null
            || !string.IsNullOrWhiteSpace(cached.ErrorMessage);
    }

    private Task RetryLoadBoardStateAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearDailyFocusBoardState();
        return LoadBoardStateAsync(
            ChromeState.Settings.PlanningBoardNodeId,
            ChromeState.Settings.Capacity,
            ChromeState.Settings.StallDays);
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

    private Task RetryLoadRecommendationsAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearDailyFocusRecommendations();
        return LoadRecommendationsAsync(ChromeState.Settings.PlanningBoardNodeId);
    }

    private async Task LoadBoardStateAsync(string boardId, int capacity, int stallDays)
    {
        if (TryApplyCachedBoardState(boardId, capacity, stallDays))
        {
            return;
        }

        isLoadingBoardState = true;
        loadErrorMessage = null;
        boardState = null;
        ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, stallDays, null, null, isLoading: true);

        try
        {
            boardState = await BoardStateService
                .GetBoardStateAsync(boardId, capacity, stallDays)
                .ConfigureAwait(false);
            ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, stallDays, boardState, null, isLoading: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus board occupancy.");
            boardState = null;
            loadErrorMessage = "Unable to load board occupancy. Check your GitHub connection and try again.";
            ChromeCoordinator.SetDailyFocusBoardState(boardId, capacity, stallDays, null, loadErrorMessage, isLoading: false);
        }
        finally
        {
            isLoadingBoardState = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
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
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
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

    private async Task LoadRecommendationsAsync(string boardId)
    {
        if (TryApplyCachedRecommendations(boardId))
        {
            return;
        }

        isLoadingRecommendations = true;
        recommendationsErrorMessage = null;
        recommendationsWarningMessage = null;
        recommendations = null;
        ChromeCoordinator.SetDailyFocusRecommendations(boardId, null, null, isLoading: true);

        try
        {
            var result = await RecommendationService.GetRecommendationsAsync(boardId).ConfigureAwait(false);
            recommendations = result.Recommendations;
            recommendationsWarningMessage = FormatPartialFailureWarning(result.Failures);
            ChromeCoordinator.SetDailyFocusRecommendations(
                boardId,
                recommendations,
                null,
                isLoading: false,
                recommendationsWarningMessage);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus recommendations.");
            recommendations = null;
            recommendationsWarningMessage = null;
            recommendationsErrorMessage =
                "Unable to load recommended work. Check your GitHub connection and try again.";
            ChromeCoordinator.SetDailyFocusRecommendations(
                boardId,
                null,
                recommendationsErrorMessage,
                isLoading: false);
        }
        finally
        {
            isLoadingRecommendations = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private string DailyFocusLoadingAriaLabel
    {
        get
        {
            var loadingParts = new List<string>();
            if (isLoadingBoardState)
            {
                loadingParts.Add("occupancy");
            }

            if (isLoadingRecommendations)
            {
                loadingParts.Add("recommendations");
            }

            if (isLoadingStalledReviews)
            {
                loadingParts.Add("stalled review pull requests");
            }

            return loadingParts.Count switch
            {
                0 => "Loading Daily Focus",
                1 => $"Loading Daily Focus {loadingParts[0]}",
                _ => $"Loading Daily Focus {string.Join(" and ", loadingParts)}",
            };
        }
    }

    private static string FormatPriorityChip(string? priorityLabel)
        => string.IsNullOrWhiteSpace(priorityLabel) ? "Unlabelled" : priorityLabel;

    private static string FormatItemReference(DailyFocusRecommendationDto recommendation)
        => $"{recommendation.RepositoryFullName}#{recommendation.Number}";

    private static string? FormatPartialFailureWarning(IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        if (failures.Count == 0)
        {
            return null;
        }

        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return $"Recommended work was ranked without {failures.Count} {noun} that failed to load: {repositories}.";
    }
}
