namespace SoloDevBoard.Application.Services;

/// <summary>Provides audit dashboard operations.</summary>
public interface IAuditDashboardService
{
    /// <summary>Retrieves audit summary counters for repositories visible to the current owner.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository audit summary counters.</returns>
    Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetRepositorySummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves open issues for the specified repository.</summary>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of open issues for the repository.</returns>
    Task<IReadOnlyList<IssueDto>> GetOpenIssuesAsync(string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves per-repository audit summary counters for the specified repositories.</summary>
    /// <param name="repos">A read-only list of repository names.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository audit summary counters.</returns>
    Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetAuditSummaryAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default);

    /// <summary>Retrieves open unlabelled issues across the specified repositories.</summary>
    /// <param name="repos">A read-only list of repository names.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of unlabelled issues.</returns>
    Task<IReadOnlyList<IssueDto>> GetUnlabelledIssuesAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default);

    /// <summary>Retrieves stale open pull requests across the specified repositories.</summary>
    /// <param name="repos">A read-only list of repository names.</param>
    /// <param name="staleDays">The number of days since the last activity after which a pull request is stale.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of stale open pull requests.</returns>
    Task<IReadOnlyList<PullRequestDto>> GetStalePullRequestsAsync(IReadOnlyList<string> repos, int staleDays = 14, CancellationToken cancellationToken = default);

    /// <summary>Retrieves failing or cancelled most recent workflow runs across the specified repositories.</summary>
    /// <param name="repos">A read-only list of repository names.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of failing or cancelled workflow runs.</returns>
    Task<IReadOnlyList<WorkflowRunDto>> GetFailingWorkflowRunsAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default);
}
