namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Per-repository open-work summary derived from the PM work-item catalogue.</summary>
/// <param name="FullName">The repository in <c>owner/name</c> form.</param>
/// <param name="OpenIssueCount">The number of open issues in the catalogue for this repository.</param>
/// <param name="OpenPullRequestCount">The number of open pull requests in the catalogue for this repository.</param>
/// <param name="LastActivityAt">The latest catalogue item update, or the repository update time when there are no open items.</param>
/// <param name="IsIncluded"><see langword="true" /> when the repository participates in PM queries; otherwise <see langword="false" />.</param>
public sealed record PmRepositorySummaryDto(
    string FullName,
    int OpenIssueCount,
    int OpenPullRequestCount,
    DateTimeOffset LastActivityAt,
    bool IsIncluded);
