namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Candidate work item that can be added to Up Next from Iteration Planning.</summary>
/// <param name="ItemType">Whether the item is an issue or pull request.</param>
/// <param name="Number">The repository-scoped item number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Labels">Label names currently applied to the item.</param>
/// <param name="BoardStatusName">The joined planning-board Status name, when the item is already on the board.</param>
/// <param name="ProjectItemId">The project-item node identifier when the item is already on the board.</param>
public sealed record IterationPlanningCandidateDto(
    PmWorkItemTypeDto ItemType,
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    IReadOnlyList<string> Labels,
    string? BoardStatusName,
    string? ProjectItemId);
