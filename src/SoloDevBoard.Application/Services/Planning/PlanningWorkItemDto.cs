namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Cross-repository PM work item at the Application→App boundary.</summary>
/// <param name="ItemType">Whether the item is an issue or pull request.</param>
/// <param name="Number">The repository-scoped item number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Labels">Label names currently applied to the item.</param>
/// <param name="MilestoneNumber">The assigned milestone number, when present.</param>
/// <param name="MilestoneTitle">The assigned milestone title, when present.</param>
/// <param name="CreatedAt">When the item was created.</param>
/// <param name="UpdatedAt">When the item was last updated.</param>
/// <param name="IsDraft">For pull requests, whether the item is a draft; <see langword="null"/> for issues.</param>
/// <param name="HasReviewPending">For pull requests, whether a review is pending; <see langword="null"/> for issues.</param>
/// <param name="SubIssueTotal">For open epics and features, the tracked sub-issue total when GitHub provides it.</param>
/// <param name="SubIssueCompleted">For open epics and features, the completed tracked sub-issue count when GitHub provides it.</param>
public sealed record PlanningWorkItemDto(
    PlanningWorkItemTypeDto ItemType,
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    IReadOnlyList<string> Labels,
    int? MilestoneNumber,
    string? MilestoneTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool? IsDraft,
    bool? HasReviewPending,
    int? SubIssueTotal,
    int? SubIssueCompleted);
