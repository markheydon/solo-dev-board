namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Represents the audit dashboard data required to generate a Markdown export.</summary>
/// <param name="RepositorySummaries">Per-repository audit summary counters.</param>
/// <param name="UnlabelledIssues">Open unlabelled issues across the selected repositories.</param>
/// <param name="StalePullRequests">Stale open pull requests across the selected repositories.</param>
/// <param name="FailingWorkflowRuns">Failing or cancelled workflow runs across the selected repositories.</param>
/// <param name="SelectedRepositories">The repository names included in the export.</param>
/// <param name="TotalOpenIssues">The total number of open issues across selected repositories.</param>
/// <param name="TotalOpenPullRequests">The total number of open pull requests across selected repositories.</param>
/// <param name="TotalUnlabelledIssues">The total number of unlabelled issues across selected repositories.</param>
/// <param name="TotalFailingWorkflows">The total number of failing workflows across selected repositories.</param>
/// <param name="StalePullRequestDays">The number of days after which a pull request is considered stale.</param>
/// <param name="GeneratedAtUtc">The UTC timestamp when the export was generated.</param>
public sealed record AuditDashboardMarkdownExportRequest(
    IReadOnlyList<RepositoryAuditSummaryDto> RepositorySummaries,
    IReadOnlyList<IssueDto> UnlabelledIssues,
    IReadOnlyList<PullRequestDto> StalePullRequests,
    IReadOnlyList<WorkflowRunDto> FailingWorkflowRuns,
    IReadOnlyList<string> SelectedRepositories,
    int TotalOpenIssues,
    int TotalOpenPullRequests,
    int TotalUnlabelledIssues,
    int TotalFailingWorkflows,
    int StalePullRequestDays,
    DateTimeOffset GeneratedAtUtc);
