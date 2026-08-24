namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Loads open issues and pull requests across included active repositories for PM views.</summary>
public interface IPlanningWorkItemCatalogueService
{
    /// <summary>
    /// Builds the PM work-item catalogue for all active repositories that are not excluded in PM settings.
    /// This call fans out to GitHub for issues, pull requests, review metadata, and sub-issue summaries.
    /// Partial per-repository failures are returned alongside successfully loaded items.
    /// Repository summaries are aggregated in memory from those items; failed repositories are omitted so counts are not shown as zero.
    /// GitHub 404 and 410 responses for issues or pull requests on a listed repository are treated as
    /// empty lists, not failures.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The catalogue items and any repository-level failures.</returns>
    Task<PlanningWorkItemCatalogueResultDto> GetCatalogueAsync(CancellationToken cancellationToken = default);
}
