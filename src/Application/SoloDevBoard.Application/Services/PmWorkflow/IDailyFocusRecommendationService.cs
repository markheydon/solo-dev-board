namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Builds the Daily Focus top-three unblocked work-item recommendations.</summary>
public interface IDailyFocusRecommendationService
{
    /// <summary>
    /// Loads the top three unblocked work items ranked by priority, then recency,
    /// honouring repository exclusions and the selected board's parked and in-progress statuses.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier for the selected planning board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// Ranked recommendations and any per-repository catalogue failures that did not prevent ranking.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The work-item catalogue reported repository failures and returned no items to rank.
    /// </exception>
    Task<DailyFocusRecommendationResultDto> GetRecommendationsAsync(
        string projectId,
        CancellationToken cancellationToken = default);
}
