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
    public IDailyFocusBoardStateService BoardStateService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowDailyFocusPanel> Logger { get; set; } = default!;

    private DailyFocusBoardStateDto? boardState;
    private bool isLoadingBoardState;
    private string? loadErrorMessage;
    private string? loadedBoardId;
    private int loadedRevision = -1;

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
            loadedBoardId = null;
            loadedRevision = DataRevision;
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId;
        if (string.Equals(loadedBoardId, boardId, StringComparison.Ordinal)
            && loadedRevision == DataRevision
            && boardState is not null
            && string.IsNullOrWhiteSpace(loadErrorMessage))
        {
            return;
        }

        await LoadBoardStateAsync();
    }

    private async Task LoadBoardStateAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return;
        }

        isLoadingBoardState = true;
        loadErrorMessage = null;
        loadedBoardId = ChromeState.Settings.PlanningBoardNodeId;
        loadedRevision = DataRevision;

        try
        {
            boardState = await BoardStateService.GetBoardStateAsync(
                ChromeState.Settings.PlanningBoardNodeId,
                ChromeState.Settings.Capacity);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Daily Focus board occupancy.");
            boardState = null;
            loadErrorMessage = "Unable to load board occupancy. Check your GitHub connection and try again.";
        }
        finally
        {
            isLoadingBoardState = false;
        }
    }
}
