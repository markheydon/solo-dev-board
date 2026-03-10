namespace SoloDevBoard.Application.Services;

/// <summary>Represents per-repository audit counters for the dashboard.</summary>
/// <param name="RepositoryFullName">The fully-qualified repository name in owner/name format.</param>
/// <param name="OpenIssueCount">The number of open issues.</param>
/// <param name="OpenPullRequestCount">The number of open pull requests.</param>
/// <param name="UnlabelledIssueCount">The number of open issues with no labels.</param>
/// <param name="StalePullRequestCount">The number of stale open pull requests.</param>
/// <param name="FailingWorkflowCount">The number of workflows whose most recent run is failing or cancelled.</param>
public sealed record RepositoryAuditSummaryDto(
    string RepositoryFullName,
    int OpenIssueCount,
    int OpenPullRequestCount,
    int UnlabelledIssueCount,
    int StalePullRequestCount,
    int FailingWorkflowCount);
