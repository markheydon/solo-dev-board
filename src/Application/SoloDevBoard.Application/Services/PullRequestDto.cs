namespace SoloDevBoard.Application.Services;

/// <summary>Represents a pull request item returned by audit dashboard operations.</summary>
/// <param name="Number">The repository-scoped pull request number.</param>
/// <param name="Title">The pull request title.</param>
/// <param name="HtmlUrl">The web URL of the pull request.</param>
/// <param name="RepositoryFullName">The fully-qualified repository name in owner/name format.</param>
/// <param name="UpdatedAt">The date and time when the pull request was last updated.</param>
public sealed record PullRequestDto(
    int Number,
    string Title,
    string HtmlUrl,
    string RepositoryFullName,
    DateTimeOffset UpdatedAt);
