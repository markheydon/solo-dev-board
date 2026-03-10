namespace SoloDevBoard.Application.Services;

/// <summary>Represents an issue item returned by audit dashboard operations.</summary>
/// <param name="Number">The repository-scoped issue number.</param>
/// <param name="Title">The issue title.</param>
/// <param name="HtmlUrl">The web URL of the issue.</param>
/// <param name="RepositoryFullName">The fully-qualified repository name in owner/name format.</param>
/// <param name="UpdatedAt">The date and time when the issue was last updated.</param>
public sealed record IssueDto(
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    DateTimeOffset UpdatedAt);
