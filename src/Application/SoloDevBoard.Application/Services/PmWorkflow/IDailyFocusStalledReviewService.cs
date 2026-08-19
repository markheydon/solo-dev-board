namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Detects pull requests waiting on review for Daily Focus.</summary>
public interface IDailyFocusStalledReviewService
{
    /// <summary>
    /// Loads stalled review pull requests for the selected planning board.
    /// Uses time in an In Review (or equivalent) Status when that column exists;
    /// otherwise uses open non-draft pull requests with a pending review.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="stallDays">Inclusive stall threshold in days; values less than 1 fall back to the default.</param>
    /// <param name="excludedRepositories">Repositories omitted from Daily Focus stall alerts, in <c>owner/name</c> form.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// The stalled review snapshot. Pending-review fallback throws
    /// <see cref="InvalidOperationException"/> when the work-item catalogue reports any repository failures,
    /// so a failed load is not presented as an empty stall list.
    /// </returns>
    Task<DailyFocusStalledReviewSnapshotDto> GetStalledReviewPullRequestsAsync(
        string projectId,
        int stallDays,
        IReadOnlyList<string> excludedRepositories,
        CancellationToken cancellationToken = default);
}
