namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Loads and updates the Iteration Planning Up Next batch on the selected planning board.</summary>
public interface IIterationPlanningService
{
    /// <summary>
    /// Loads the current Up Next batch and cross-repository candidates for the selected planning board.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The planning view snapshot.</returns>
    /// <exception cref="InvalidOperationException">
    /// The work-item catalogue reported repository failures and returned no items to display.
    /// </exception>
    Task<IterationPlanningViewDto> GetPlanningViewAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a work item to Up Next on the selected planning board, creating a board card when needed.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="itemType">Whether the item is an issue or pull request.</param>
    /// <param name="repositoryFullName">The repository in <c>owner/name</c> form.</param>
    /// <param name="number">The repository-scoped item number.</param>
    /// <param name="labels">Label names on the work item, used to decide Focus Order assignment.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The add outcome, including any Focus Order assigned.</returns>
    Task<IterationPlanningAddToUpNextResultDto> AddToUpNextAsync(
        string projectId,
        PmWorkItemTypeDto itemType,
        string repositoryFullName,
        int number,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default);
}
