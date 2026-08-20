namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Stalled Up Next row on the Iteration Planning page.</summary>
/// <param name="ProjectItemId">The project-item node identifier.</param>
/// <param name="ItemType">Whether the linked content is an issue or pull request.</param>
/// <param name="Number">The repository-scoped item number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="AgeInDays">Whole days since the stall clock started.</param>
/// <param name="UsedUpdatedAtFallback">Whether age used item last-updated time.</param>
/// <param name="Labels">Label names currently applied to the item.</param>
public sealed record IterationPlanningStalledItemDto(
    string ProjectItemId,
    PmWorkItemTypeDto ItemType,
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    int AgeInDays,
    bool UsedUpdatedAtFallback,
    IReadOnlyList<string> Labels);
