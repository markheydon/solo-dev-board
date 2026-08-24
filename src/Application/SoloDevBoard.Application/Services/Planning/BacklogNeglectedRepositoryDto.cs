namespace SoloDevBoard.Application.Services.Planning;

/// <summary>A repository with no recent issue or pull request activity in Backlog Review.</summary>
/// <param name="FullName">The repository in <c>owner/name</c> form.</param>
/// <param name="LastActivityAt">The latest catalogue item update, or <see langword="null"/> when no activity was recorded.</param>
/// <param name="OpenIssueCount">The number of open issues in the catalogue for this repository.</param>
/// <param name="OpenPullRequestCount">The number of open pull requests in the catalogue for this repository.</param>
public sealed record BacklogNeglectedRepositoryDto(
    string FullName,
    DateTimeOffset? LastActivityAt,
    int OpenIssueCount,
    int OpenPullRequestCount);
