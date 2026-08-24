namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Loads and updates the Iteration Planning Up Next batch on the selected planning board.</summary>
public interface IIterationPlanningService
{
    /// <summary>
    /// Loads the current Up Next batch and cross-repository candidates for the selected planning board.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="capacity">The persisted planning capacity from PM settings.</param>
    /// <param name="stallDays">The inclusive stall threshold in days from PM settings.</param>
    /// <param name="forceReload">When <see langword="true" />, invalidates the cached project board catalogue before loading.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The planning view snapshot.</returns>
    /// <exception cref="InvalidOperationException">
    /// The work-item catalogue reported repository failures and returned no items to display.
    /// </exception>
    Task<IterationPlanningViewDto> GetPlanningViewAsync(
        string projectId,
        int capacity,
        int stallDays,
        bool forceReload = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a work item to Up Next on the selected planning board, creating a board card when needed.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="itemType">Whether the item is an issue or pull request.</param>
    /// <param name="repositoryFullName">The repository in <c>owner/name</c> form.</param>
    /// <param name="number">The repository-scoped item number.</param>
    /// <param name="labels">Label names on the work item, used to decide Focus Order assignment.</param>
    /// <param name="stallDays">The inclusive stall threshold in days from PM settings.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The add outcome, including any Focus Order assigned.</returns>
    Task<IterationPlanningAddToUpNextResultDto> AddToUpNextAsync(
        string projectId,
        PmWorkItemTypeDto itemType,
        string repositoryFullName,
        int number,
        IReadOnlyList<string> labels,
        int stallDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the stall clock for a stalled Up Next item by moving it away from Up Next and back.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task ReCommitStalledUpNextItemAsync(
        string projectId,
        string projectItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a stalled Up Next item as blocked on the board and applies <c>status/blocked</c> on the work item.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="item">The stalled Up Next item to update.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task MarkStalledUpNextItemBlockedAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a stalled Up Next item to Ice Box on the board, applies <c>status/ice-box</c>, and clears Focus Order.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="item">The stalled Up Next item to update.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task MoveStalledUpNextItemToIceBoxAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a stalled Up Next item to Todo on the board and clears Focus Order.
    /// </summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="item">The stalled Up Next item to remove from the batch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task RemoveStalledUpNextItemAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the union of milestone titles available on the selected Up Next items' repositories.
    /// </summary>
    /// <param name="selectedItems">Checked Up Next items in the current batch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Deduplicated milestone options ordered by title.</returns>
    Task<IReadOnlyList<IterationPlanningMilestoneOptionDto>> GetBulkMilestoneOptionsAsync(
        IReadOnlyList<IterationPlanningUpNextItemDto> selectedItems,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a milestone title to the selected Up Next items, skipping repositories where it is missing.
    /// </summary>
    /// <param name="selectedItems">Checked Up Next items in the current batch.</param>
    /// <param name="milestoneTitle">The milestone title to assign.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The apply outcome, including repositories skipped.</returns>
    Task<IterationPlanningBulkMilestoneResultDto> ApplyBulkMilestoneAsync(
        IReadOnlyList<IterationPlanningUpNextItemDto> selectedItems,
        string milestoneTitle,
        CancellationToken cancellationToken = default);
}
