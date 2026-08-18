namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Loads open issues and pull requests across included active repositories for PM views.</summary>
public interface IPmWorkItemCatalogueService
{
    /// <summary>
    /// Builds the PM work-item catalogue for all active repositories that are not excluded in PM settings.
    /// Partial per-repository failures are returned alongside successfully loaded items.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The catalogue items and any repository-level failures.</returns>
    Task<PmWorkItemCatalogueResultDto> GetCatalogueAsync(CancellationToken cancellationToken = default);
}
