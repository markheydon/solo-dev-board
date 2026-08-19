using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Daily Focus occupancy and recommendation panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
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
    public IDailyFocusRecommendationService RecommendationService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowDailyFocusPanel> Logger { get; set; } = default!;

    private DailyFocusBoardStateDto? boardState;
    private IReadOnlyList<DailyFocusRecommendationDto>? recommendations;
    private bool isLoadingBoardState;
    private bool isLoadingRecommendations;
    private string? loadErrorMessage;
    private string? recommendationsErrorMessage;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (ChromeState is null || ChromeState.IsLoading)
        {
            return;
        }

        if (!ChromeState.HasPlanningBoardSelected)
        {
            boardState = null;
            recommendations = null;
            loadErrorMessage = null;
            recommendationsErrorMessage = null;
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        var capacity = ChromeState.Settings.Capacity;
        var boardCached = TryApplyCachedBoardState(boardId, capacity);
        var recommendationsCached = TryApplyCachedRecommendations(boardId);

        var loadBoard = boardCached
            ? Task.CompletedTask
            : LoadBoardStateAsync(boardId, capacity);
        var loadRecommendations = recommendationsCached
            ? Task.CompletedTask
            : LoadRecommendationsAsync(boardId);

        await Task.WhenAll(loadBoard, loadRecommendations).ConfigureAwait(false);
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
        return LoadBoardStateAsync(ChromeState.Settings.PlanningBoardNodeId, ChromeState.Settings.Capacity);
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

    private async Task LoadRecommendationsAsync(string boardId)
    {
        if (TryApplyCachedRecommendations(boardId))
        {
            return;
        }

        isLoadingRecommendations = true;
        recommendationsErrorMessage = null;
        recommendations = null;
        ChromeCoordinator.SetDailyFocusRecommendations(boardId, null, null, isLoading: true);

        try
        {
            recommendations = await RecommendationService.GetRecommendationsAsync(boardId).ConfigureAwait(false);
            ChromeCoordinator.SetDailyFocusRecommendations(boardId, recommendations, null, isLoading: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus recommendations.");
            recommendations = null;
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
        }
    }

    private static string FormatPriorityChip(string? priorityLabel)
        => string.IsNullOrWhiteSpace(priorityLabel) ? "Unlabelled" : priorityLabel;

    private static string FormatItemReference(DailyFocusRecommendationDto recommendation)
        => $"{recommendation.RepositoryFullName}#{recommendation.Number}";
}
