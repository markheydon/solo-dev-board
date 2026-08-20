namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>A Backlog Review row at the Application→App boundary.</summary>
/// <param name="ItemType">Whether the item is an issue or pull request.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Number">The repository-scoped issue or pull request number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item on GitHub.</param>
/// <param name="Labels">Label names currently applied to the item.</param>
/// <param name="PriorityLabel">The <c>priority/</c> label name, or <see langword="null"/> when unlabelled.</param>
/// <param name="BoardStatusName">The joined planning-board Status name, or <see langword="null"/> when the item is not on the board.</param>
public sealed record BacklogReviewItemDto(
    PmWorkItemTypeDto ItemType,
    string RepositoryFullName,
    int Number,
    string Title,
    string HtmlUrl,
    IReadOnlyList<string> Labels,
    string? PriorityLabel,
    string? BoardStatusName);
