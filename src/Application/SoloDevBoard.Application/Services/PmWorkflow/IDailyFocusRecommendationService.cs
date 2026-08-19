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
    /// <returns>Up to three ranked recommendations.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the work-item catalogue reports one or more repository failures, including an empty item set.
    /// </exception>
    Task<IReadOnlyList<DailyFocusRecommendationDto>> GetRecommendationsAsync(
        string projectId,
        CancellationToken cancellationToken = default);
}
