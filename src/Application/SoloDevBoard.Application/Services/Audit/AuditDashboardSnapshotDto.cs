namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Represents a consolidated audit dashboard snapshot for the selected repositories.</summary>
/// <param name="RepositorySummaries">Per-repository audit summary counters.</param>
/// <param name="UnlabelledIssues">Open issues with no labels across the selected repositories.</param>
/// <param name="StalePullRequests">Stale open pull requests across the selected repositories.</param>
/// <param name="FailingWorkflowRuns">Failing or cancelled most recent workflow runs across the selected repositories.</param>
public sealed record AuditDashboardSnapshotDto(
    IReadOnlyList<RepositoryAuditSummaryDto> RepositorySummaries,
    IReadOnlyList<IssueDto> UnlabelledIssues,
    IReadOnlyList<PullRequestDto> StalePullRequests,
    IReadOnlyList<WorkflowRunDto> FailingWorkflowRuns);
