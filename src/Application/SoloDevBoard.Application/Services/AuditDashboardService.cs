using SoloDevBoard.Application.Identity;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

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
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetRepositorySummaryAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerLogin();
        var repositories = await _gitHubService.GetActiveRepositoriesAsync(owner, cancellationToken).ConfigureAwait(false);
        var repoNames = repositories.Select(static repository => repository.Name).ToArray();

        return await GetAuditSummaryAsync(repoNames, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IssueDto>> GetOpenIssuesAsync(string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var owner = GetOwnerLogin();
        var repoName = NormaliseRepoName(repo);
        var repositoryFullName = BuildRepositoryFullName(owner, repoName);
        var issues = await _gitHubService.GetIssuesAsync(owner, repoName, cancellationToken).ConfigureAwait(false);

        return issues
            .Where(static issue => IsOpenState(issue.State))
            .Select(issue => MapIssue(issue, repositoryFullName))
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryAuditSummaryDto>> GetAuditSummaryAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var owner = GetOwnerLogin();
        var repoNames = GetRepoNames(repos);
        var summaryTasks = repoNames.Select(repoName => BuildRepositoryAuditSummaryAsync(owner, repoName, cancellationToken));
        var summaries = await Task.WhenAll(summaryTasks).ConfigureAwait(false);

        return summaries;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IssueDto>> GetUnlabelledIssuesAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var owner = GetOwnerLogin();
        var repoNames = GetRepoNames(repos);
        var issueTasks = repoNames.Select(async repoName =>
        {
            var issues = await _gitHubService.GetIssuesAsync(owner, repoName, cancellationToken).ConfigureAwait(false);
            var repositoryFullName = BuildRepositoryFullName(owner, repoName);

            return issues
                .Where(static issue => IsOpenState(issue.State) && issue.Labels.Count == 0)
                .Select(issue => MapIssue(issue, repositoryFullName))
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

        var owner = GetOwnerLogin();
        var staleBefore = DateTimeOffset.UtcNow.AddDays(-staleDays);
        var repoNames = GetRepoNames(repos);
        var pullRequestTasks = repoNames.Select(async repoName =>
        {
            var pullRequests = await _gitHubService.GetPullRequestsAsync(owner, repoName, cancellationToken).ConfigureAwait(false);
            var repositoryFullName = BuildRepositoryFullName(owner, repoName);

            return pullRequests
                .Where(pullRequest => pullRequest.State.Equals("open", StringComparison.OrdinalIgnoreCase) && pullRequest.UpdatedAt < staleBefore)
                .Select(pullRequest => MapPullRequest(pullRequest, repositoryFullName))
                .ToArray();
        });

        var pullRequestsByRepository = await Task.WhenAll(pullRequestTasks).ConfigureAwait(false);
        return pullRequestsByRepository.SelectMany(static pullRequests => pullRequests).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowRunDto>> GetFailingWorkflowRunsAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var owner = GetOwnerLogin();
        var repoNames = GetRepoNames(repos);
        var workflowTasks = repoNames.Select(async repoName =>
        {
            var workflowRuns = await _gitHubService.GetWorkflowRunsAsync(owner, repoName, cancellationToken).ConfigureAwait(false);
            var repositoryFullName = BuildRepositoryFullName(owner, repoName);

            return workflowRuns
                .Where(static workflowRun => !string.IsNullOrWhiteSpace(workflowRun.WorkflowName))
                .GroupBy(static workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
                .Select(static workflowGroup => workflowGroup
                    .OrderByDescending(static workflowRun => workflowRun.UpdatedAt)
                    .ThenByDescending(static workflowRun => workflowRun.CreatedAt)
                    .First())
                .Where(static workflowRun => IsFailingConclusion(workflowRun.Conclusion))
                .Select(workflowRun => MapWorkflowRun(workflowRun, repositoryFullName))
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

    private static IReadOnlyList<string> GetRepoNames(IReadOnlyList<string> repos)
    {
        if (repos.Count == 0)
        {
            return [];
        }

        return repos.Select(NormaliseRepoName).ToArray();
    }

    private static string NormaliseRepoName(string repo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var trimmed = repo.Trim();
        var separatorIndex = trimmed.IndexOf('/');
        if (separatorIndex >= 0)
        {
            var repoName = trimmed[(separatorIndex + 1)..];
            ArgumentException.ThrowIfNullOrWhiteSpace(repoName);
            return repoName;
        }

        return trimmed;
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
}
