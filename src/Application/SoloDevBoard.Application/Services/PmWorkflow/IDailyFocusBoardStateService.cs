namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Builds the Daily Focus board-state snapshot for a selected planning board.</summary>
public interface IDailyFocusBoardStateService
{
    /// <summary>Loads occupancy chips and active load for the specified project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="capacity">The persisted planning capacity used as the active-load denominator.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Daily Focus board snapshot.</returns>
    Task<DailyFocusBoardStateDto> GetBoardStateAsync(
        string projectId,
        int capacity,
        CancellationToken cancellationToken = default);
}
