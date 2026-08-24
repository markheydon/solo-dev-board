namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Checks whether a selected planning board exposes the fields and Status options Planning expects.</summary>
public interface IPlanningBoardCompatibilityService
{
    /// <summary>
    /// Loads the project board catalogue and evaluates it against Planning expectations.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="forceReload">When <see langword="true" />, bypasses any cached catalogue for the board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The compatibility report for the board.</returns>
    Task<PlanningBoardCompatibilityReportDto> GetReportAsync(
        string projectId,
        bool forceReload = false,
        CancellationToken cancellationToken = default);
}
