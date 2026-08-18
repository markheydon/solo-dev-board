namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Provides PM workflow access to GitHub Project v2 board item catalogues.</summary>
public interface IProjectItemCatalogueService
{
    /// <summary>Retrieves all items and discovered field identifiers for a project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The project board item catalogue.</returns>
    Task<ProjectBoardItemCatalogueDto> GetCatalogueAsync(string projectId, CancellationToken cancellationToken = default);

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
