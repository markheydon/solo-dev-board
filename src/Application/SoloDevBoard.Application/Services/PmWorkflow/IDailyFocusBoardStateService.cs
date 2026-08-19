namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Builds the Daily Focus board-state snapshot for a selected planning board.</summary>
public interface IDailyFocusBoardStateService
{
    /// <summary>Loads occupancy chips, active load, and stalled Up Next items for the specified project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="capacity">The persisted planning capacity used as the active-load denominator.</param>
    /// <param name="stallDays">The inclusive stall threshold in days for Up Next items.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Daily Focus board snapshot.</returns>
    Task<DailyFocusBoardStateDto> GetBoardStateAsync(
        string projectId,
        int capacity,
        int stallDays,
        CancellationToken cancellationToken = default);
}
