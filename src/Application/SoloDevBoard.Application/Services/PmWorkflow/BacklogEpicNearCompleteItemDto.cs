namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>An open epic or feature whose tracked sub-issues are all closed.</summary>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Number">The repository-scoped issue number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item on GitHub.</param>
/// <param name="TypeLabel">The <c>type/epic</c> or <c>type/feature</c> label name.</param>
/// <param name="SubIssueTotal">The tracked sub-issue total from GitHub.</param>
/// <param name="SubIssueCompleted">The completed tracked sub-issue count from GitHub.</param>
public sealed record BacklogEpicNearCompleteItemDto(
    string RepositoryFullName,
    int Number,
    string Title,
    string HtmlUrl,
    string TypeLabel,
    int SubIssueTotal,
    int SubIssueCompleted);
