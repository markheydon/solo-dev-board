using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Provides audit dashboard operations over multiple repositories.</summary>
public sealed class AuditDashboardService : IAuditDashboardService
{
    private const int DefaultStaleDays = 14;

    private readonly IGitHubService _gitHubService;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>Initialises a new instance of the <see cref="AuditDashboardService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used for repository data retrieval.</param>
    /// <param name="currentUserContext">The current user context used to resolve the owner login.</param>
    public AuditDashboardService(IGitHubService gitHubService, ICurrentUserContext currentUserContext)
    {
        ArgumentNullException.ThrowIfNull(gitHubService);
        ArgumentNullException.ThrowIfNull(currentUserContext);

        _gitHubService = gitHubService;
        _currentUserContext = currentUserContext;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetRepositorySummaryAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await _gitHubService.GetActiveRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        var repoNames = repositories.Select(static repository => repository.FullName).ToArray();

        return await GetAuditSummaryAsync(repoNames, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IssueDto>> GetOpenIssuesAsync(string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var repositoryReference = ResolveRepositoryReference(repo);
        var issues = await _gitHubService
            .GetIssuesAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken)
            .ConfigureAwait(false);

        return issues
            .Where(static issue => IsOpenState(issue.State))
            .Select(issue => MapIssue(issue, repositoryReference.FullName))
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetAuditSummaryAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var repositoryReferences = GetRepositoryReferences(repos);
        var summaryTasks = repositoryReferences.Select(repositoryReference =>
            BuildRepositoryAuditSummaryAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken));
        var summaries = await Task.WhenAll(summaryTasks).ConfigureAwait(false);

        return summaries;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IssueDto>> GetUnlabelledIssuesAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var repositoryReferences = GetRepositoryReferences(repos);
        var issueTasks = repositoryReferences.Select(async repositoryReference =>
        {
            var issues = await _gitHubService
                .GetIssuesAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken)
                .ConfigureAwait(false);

            return issues
                .Where(static issue => IsOpenState(issue.State) && issue.Labels.Count == 0)
                .Select(issue => MapIssue(issue, repositoryReference.FullName))
                .ToArray();
        });

        var issuesByRepository = await Task.WhenAll(issueTasks).ConfigureAwait(false);
        return issuesByRepository.SelectMany(static issues => issues).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PullRequestDto>> GetStalePullRequestsAsync(IReadOnlyList<string> repos, int staleDays = DefaultStaleDays, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        if (staleDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(staleDays), staleDays, "Stale days must be greater than zero.");
        }

        var staleBefore = DateTimeOffset.UtcNow.AddDays(-staleDays);
        var repositoryReferences = GetRepositoryReferences(repos);
        var pullRequestTasks = repositoryReferences.Select(async repositoryReference =>
        {
            var pullRequests = await _gitHubService
                .GetPullRequestsAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken)
                .ConfigureAwait(false);

            return pullRequests
                .Where(pullRequest => pullRequest.State.Equals("open", StringComparison.OrdinalIgnoreCase) && pullRequest.UpdatedAt < staleBefore)
                .Select(pullRequest => MapPullRequest(pullRequest, repositoryReference.FullName))
                .ToArray();
        });

        var pullRequestsByRepository = await Task.WhenAll(pullRequestTasks).ConfigureAwait(false);
        return pullRequestsByRepository.SelectMany(static pullRequests => pullRequests).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowRunDto>> GetFailingWorkflowRunsAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var repositoryReferences = GetRepositoryReferences(repos);
        var workflowTasks = repositoryReferences.Select(async repositoryReference =>
        {
            var workflowRuns = await _gitHubService
                .GetWorkflowRunsAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken)
                .ConfigureAwait(false);

            return workflowRuns
                .Where(static workflowRun => !string.IsNullOrWhiteSpace(workflowRun.WorkflowName))
                .GroupBy(static workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
                .Select(static workflowGroup => workflowGroup
                    .OrderByDescending(static workflowRun => workflowRun.UpdatedAt)
                    .ThenByDescending(static workflowRun => workflowRun.CreatedAt)
                    .First())
                .Where(static workflowRun => IsFailingConclusion(workflowRun.Conclusion))
                .Select(workflowRun => MapWorkflowRun(workflowRun, repositoryReference.FullName))
                .ToArray();
        });

        var workflowRunsByRepository = await Task.WhenAll(workflowTasks).ConfigureAwait(false);
        return workflowRunsByRepository.SelectMany(static workflowRuns => workflowRuns).ToArray();
    }

    private static IssueDto MapIssue(Issue issue, string repositoryFullName)
        => new(
            issue.Number,
            issue.Title,
            issue.HtmlUrl,
            repositoryFullName,
            issue.CreatedAt,
            issue.UpdatedAt);

    private static PullRequestDto MapPullRequest(PullRequest pullRequest, string repositoryFullName)
        => new(
            pullRequest.Number,
            pullRequest.Title,
            pullRequest.HtmlUrl,
            repositoryFullName,
            pullRequest.AuthorLogin,
            pullRequest.UpdatedAt);

    private static WorkflowRunDto MapWorkflowRun(WorkflowRun workflowRun, string repositoryFullName)
        => new(
            workflowRun.WorkflowName,
            workflowRun.Status,
            workflowRun.Conclusion,
            workflowRun.HtmlUrl,
            repositoryFullName,
            workflowRun.HeadBranch);

    private static bool IsFailingConclusion(string conclusion)
        => conclusion.Equals("failure", StringComparison.OrdinalIgnoreCase)
           || conclusion.Equals("cancelled", StringComparison.OrdinalIgnoreCase);

    private static bool IsOpenState(string state)
        => state.Equals("open", StringComparison.OrdinalIgnoreCase);

    private static string BuildRepositoryFullName(string owner, string repoName)
        => $"{owner}/{repoName}";

    private IReadOnlyList<RepositoryReference> GetRepositoryReferences(IReadOnlyList<string> repos)
    {
        if (repos.Count == 0)
        {
            return [];
        }

        return repos
            .Select(ResolveRepositoryReference)
            .DistinctBy(static repositoryReference => repositoryReference.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private RepositoryReference ResolveRepositoryReference(string repo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var trimmed = repo.Trim();
        var separatorIndex = trimmed.IndexOf('/');
        if (separatorIndex >= 0)
        {
            var owner = trimmed[..separatorIndex];
            var repoName = trimmed[(separatorIndex + 1)..];

            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            ArgumentException.ThrowIfNullOrWhiteSpace(repoName);
            return new RepositoryReference(owner, repoName);
        }

        return new RepositoryReference(GetOwnerLogin(), trimmed);
    }

    private string GetOwnerLogin()
    {
        var owner = _currentUserContext.OwnerLogin;
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        return owner;
    }

    private async Task<RepositoryAuditSummaryDto> BuildRepositoryAuditSummaryAsync(string owner, string repoName, CancellationToken cancellationToken)
    {
        var repositoryFullName = BuildRepositoryFullName(owner, repoName);
        var issuesTask = _gitHubService.GetIssuesAsync(owner, repoName, cancellationToken);
        var pullRequestsTask = _gitHubService.GetPullRequestsAsync(owner, repoName, cancellationToken);
        var workflowRunsTask = _gitHubService.GetWorkflowRunsAsync(owner, repoName, cancellationToken);

        await Task.WhenAll(issuesTask, pullRequestsTask, workflowRunsTask).ConfigureAwait(false);

        var issues = await issuesTask.ConfigureAwait(false);
        var pullRequests = await pullRequestsTask.ConfigureAwait(false);
        var workflowRuns = await workflowRunsTask.ConfigureAwait(false);

        var openIssues = issues.Where(static issue => IsOpenState(issue.State)).ToArray();
        var openPullRequests = pullRequests.Where(static pullRequest => IsOpenState(pullRequest.State)).ToArray();

        var failingWorkflowCount = workflowRuns
            .Where(static workflowRun => !string.IsNullOrWhiteSpace(workflowRun.WorkflowName))
            .GroupBy(static workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
            .Select(static workflowGroup => workflowGroup
                .OrderByDescending(static workflowRun => workflowRun.UpdatedAt)
                .ThenByDescending(static workflowRun => workflowRun.CreatedAt)
                .First())
            .Count(static workflowRun => IsFailingConclusion(workflowRun.Conclusion));

        var staleThreshold = DateTimeOffset.UtcNow.AddDays(-DefaultStaleDays);

        return new RepositoryAuditSummaryDto(
            repositoryFullName,
            openIssues.Length,
            openPullRequests.Length,
            openIssues.Count(static issue => issue.Labels.Count == 0),
            openPullRequests.Count(pullRequest => pullRequest.UpdatedAt < staleThreshold),
            failingWorkflowCount);
    }

    private sealed record RepositoryReference(string Owner, string RepoName)
    {
        public string FullName => BuildRepositoryFullName(Owner, RepoName);
    }
}
