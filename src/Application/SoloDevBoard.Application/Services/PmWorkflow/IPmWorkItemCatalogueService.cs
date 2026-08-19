namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Loads open issues and pull requests across included active repositories for PM views.</summary>
public interface IPmWorkItemCatalogueService
{
    /// <summary>
    /// Builds the PM work-item catalogue for all active repositories that are not excluded in PM settings.
    /// This call fans out to GitHub for issues, pull requests, review metadata, and sub-issue summaries.
    /// Partial per-repository failures are returned alongside successfully loaded items.
    /// Repository summaries are aggregated in memory from those items; failed repositories are omitted so counts are not shown as zero.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The catalogue items and any repository-level failures.</returns>
    Task<PmWorkItemCatalogueResultDto> GetCatalogueAsync(CancellationToken cancellationToken = default);
}
