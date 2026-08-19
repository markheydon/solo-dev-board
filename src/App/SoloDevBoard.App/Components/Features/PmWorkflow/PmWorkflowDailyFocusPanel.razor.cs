using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Daily Focus occupancy panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
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
    public ILogger<PmWorkflowDailyFocusPanel> Logger { get; set; } = default!;

    private DailyFocusBoardStateDto? boardState;
    private bool isLoadingBoardState;
    private string? loadErrorMessage;

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
            loadErrorMessage = null;
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        var capacity = ChromeState.Settings.Capacity;
        if (TryApplyCachedBoardState(boardId, capacity))
        {
            return;
        }

        await LoadBoardStateAsync(boardId, capacity).ConfigureAwait(false);
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

    private Task RetryLoadBoardStateAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearDailyFocusBoardState();
        return LoadBoardStateAsync(ChromeState.Settings.PlanningBoardNodeId, ChromeState.Settings.Capacity);
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
}
