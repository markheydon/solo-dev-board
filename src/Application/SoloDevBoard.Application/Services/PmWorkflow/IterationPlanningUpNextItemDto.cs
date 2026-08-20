namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Up Next row on the Iteration Planning page.</summary>
/// <param name="ProjectItemId">The project-item node identifier.</param>
/// <param name="ItemType">Whether the linked content is an issue or pull request.</param>
/// <param name="Number">The repository-scoped item number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="FocusOrder">The Focus Order value when assigned; otherwise, <see langword="null" />.</param>
/// <param name="Labels">Label names currently applied to the item.</param>
public sealed record IterationPlanningUpNextItemDto(
    string ProjectItemId,
    PmWorkItemTypeDto ItemType,
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    double? FocusOrder,
    IReadOnlyList<string> Labels);
