namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Provides PM workflow access to GitHub Project v2 board item catalogues.</summary>
public interface IProjectItemCatalogueService
{
    /// <summary>Retrieves all items and discovered field identifiers for a project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The project board item catalogue.</returns>
    /// <remarks>
    /// Implementations may cache a successful catalogue for the current DI scope so occupancy and recommendation
    /// ranking share one Projects v2 round-trip. Failed loads must not be cached.
    /// </remarks>
    Task<ProjectBoardItemCatalogueDto> GetCatalogueAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>Drops any cached catalogue for the project so the next load fetches fresh board data.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    void InvalidateCatalogue(string projectId);

    /// <summary>Sets the Focus Order number on a project board item.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="focusOrderFieldId">The Focus Order field node identifier discovered from the board.</param>
    /// <param name="focusOrder">The Focus Order value to set.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task UpdateFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        double focusOrder,
        CancellationToken cancellationToken = default);

    /// <summary>Clears the Focus Order number on a project board item.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="focusOrderFieldId">The Focus Order field node identifier discovered from the board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous clear operation.</returns>
    Task ClearFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        CancellationToken cancellationToken = default);
}
