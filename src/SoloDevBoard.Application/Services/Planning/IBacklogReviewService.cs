namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Groups open work from included repositories into Backlog Review urgency panels.</summary>
public interface IBacklogReviewService
{
    /// <summary>
    /// Loads open issues and pull requests from included repositories and groups them for Backlog Review.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier for the selected planning board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// Grouped items and any per-repository catalogue failures that did not prevent grouping.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The work-item catalogue reported repository failures and returned no items to group.
    /// </exception>
    Task<BacklogReviewResultDto> GetBacklogAsync(
        string projectId,
        CancellationToken cancellationToken = default);
}
