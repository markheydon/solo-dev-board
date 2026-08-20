using System.Net;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Provides audit dashboard operations over multiple repositories.</summary>
public sealed class AuditDashboardService : IAuditDashboardService
{
    private const int DefaultStaleDays = 14;
    private const string OpenItemState = "open";

    private readonly IGitHubService _gitHubService;
    private readonly ILabelRepository _labelRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<AuditDashboardService> _logger;

    /// <summary>Initialises a new instance of the <see cref="AuditDashboardService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used for repository data retrieval.</param>
    /// <param name="labelRepository">The repository used to retrieve GitHub labels for consistency analysis.</param>
    /// <param name="currentUserContext">The current user context used to resolve the owner login.</param>
    /// <param name="logger">The logger used for audit dashboard diagnostics.</param>
    public AuditDashboardService(
        IGitHubService gitHubService,
        ILabelRepository labelRepository,
        ICurrentUserContext currentUserContext,
        ILogger<AuditDashboardService> logger)
    {
        ArgumentNullException.ThrowIfNull(gitHubService);
        ArgumentNullException.ThrowIfNull(labelRepository);
        ArgumentNullException.ThrowIfNull(currentUserContext);
        ArgumentNullException.ThrowIfNull(logger);

        _gitHubService = gitHubService;
        _labelRepository = labelRepository;
        _currentUserContext = currentUserContext;
        _logger = logger;
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
            .GetIssuesAsync(repositoryReference.Owner, repositoryReference.RepoName, OpenItemState, cancellationToken)
            .ConfigureAwait(false);

        return issues
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
    public async Task<AuditDashboardSnapshotDto> GetDashboardSnapshotAsync(
        IReadOnlyList<string> repos,
        int staleDays = DefaultStaleDays,
        bool includeWorkflowRuns = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        if (staleDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(staleDays), staleDays, "Stale days must be greater than zero.");
        }

        var repositoryReferences = GetRepositoryReferences(repos);
        var dataTasks = repositoryReferences.Select(repositoryReference =>
            LoadRepositoryAuditDataAsync(repositoryReference.Owner, repositoryReference.RepoName, includeWorkflowRuns, cancellationToken));
        var dataByRepository = await Task.WhenAll(dataTasks).ConfigureAwait(false);

        var staleBefore = DateTimeOffset.UtcNow.AddDays(-staleDays);
        var summaries = new List<RepositoryAuditSummaryDto>(dataByRepository.Length);
        var unlabelledIssues = new List<IssueDto>();
        var stalePullRequests = new List<PullRequestDto>();
        var failingWorkflowRuns = new List<WorkflowRunDto>();

        foreach (var repositoryData in dataByRepository)
        {
            summaries.Add(BuildRepositoryAuditSummary(repositoryData, staleBefore));
            unlabelledIssues.AddRange(BuildUnlabelledIssues(repositoryData));
            stalePullRequests.AddRange(BuildStalePullRequests(repositoryData, staleBefore));
            failingWorkflowRuns.AddRange(BuildFailingWorkflowRuns(repositoryData));
        }

        return new AuditDashboardSnapshotDto(
            summaries.ToArray(),
            unlabelledIssues.ToArray(),
            stalePullRequests.ToArray(),
            failingWorkflowRuns.ToArray());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IssueDto>> GetUnlabelledIssuesAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var repositoryReferences = GetRepositoryReferences(repos);
        var issueTasks = repositoryReferences.Select(async repositoryReference =>
        {
            var issues = await _gitHubService
                .GetIssuesAsync(repositoryReference.Owner, repositoryReference.RepoName, OpenItemState, cancellationToken)
                .ConfigureAwait(false);

            return issues
                .Where(static issue => issue.Labels.Count == 0)
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
                .GetPullRequestsAsync(repositoryReference.Owner, repositoryReference.RepoName, OpenItemState, cancellationToken)
                .ConfigureAwait(false);

            return pullRequests
                .Where(pullRequest => pullRequest.UpdatedAt < staleBefore)
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
            var repositoryFullName = BuildRepositoryFullName(repositoryReference.Owner, repositoryReference.RepoName);
            var workflowRuns = await GetRepositoryResourceSafeAsync(
                () => _gitHubService.GetWorkflowRunsAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken),
                repositoryFullName,
                "workflow runs").ConfigureAwait(false);

            return BuildFailingWorkflowRuns(repositoryFullName, workflowRuns);
        });

        var workflowRunsByRepository = await Task.WhenAll(workflowTasks).ConfigureAwait(false);
        return workflowRunsByRepository.SelectMany(static workflowRuns => workflowRuns).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelConsistencyWarningDto>> GetLabelConsistencyWarningsAsync(IReadOnlyList<string> repos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repos);

        var repositoryReferences = GetRepositoryReferences(repos);
        var taxonomyLabels = RecommendedLabelTaxonomyCatalog.SoloDevBoard;
        var warningTasks = repositoryReferences.Select(repositoryReference =>
            BuildLabelConsistencyWarningsAsync(repositoryReference, taxonomyLabels, cancellationToken));
        var warningsByRepository = await Task.WhenAll(warningTasks).ConfigureAwait(false);

        return warningsByRepository
            .SelectMany(static warnings => warnings)
            .OrderBy(static warning => warning.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static warning => warning.LabelName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private async Task<RepositoryAuditData> LoadRepositoryAuditDataAsync(
        string owner,
        string repoName,
        bool includeWorkflowRuns,
        CancellationToken cancellationToken)
    {
        var repositoryFullName = BuildRepositoryFullName(owner, repoName);
        var issuesTask = GetRepositoryResourceSafeAsync(
            () => _gitHubService.GetIssuesAsync(owner, repoName, OpenItemState, cancellationToken),
            repositoryFullName,
            "issues");
        var pullRequestsTask = GetRepositoryResourceSafeAsync(
            () => _gitHubService.GetPullRequestsAsync(owner, repoName, OpenItemState, cancellationToken),
            repositoryFullName,
            "pull requests");
        Task<IReadOnlyList<WorkflowRun>> workflowRunsTask = includeWorkflowRuns
            ? GetRepositoryResourceSafeAsync(
                () => _gitHubService.GetWorkflowRunsAsync(owner, repoName, cancellationToken),
                repositoryFullName,
                "workflow runs")
            : Task.FromResult<IReadOnlyList<WorkflowRun>>([]);

        await Task.WhenAll(issuesTask, pullRequestsTask, workflowRunsTask).ConfigureAwait(false);

        return new RepositoryAuditData(
            repositoryFullName,
            await issuesTask.ConfigureAwait(false),
            await pullRequestsTask.ConfigureAwait(false),
            await workflowRunsTask.ConfigureAwait(false));
    }

    private async Task<IReadOnlyList<T>> GetRepositoryResourceSafeAsync<T>(
        Func<Task<IReadOnlyList<T>>> fetch,
        string repositoryFullName,
        string resourceName)
    {
        try
        {
            return await fetch().ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (IsSkippableGitHubStatus(ex.StatusCode))
        {
            _logger.LogWarning(
                ex,
                "Skipping {ResourceName} for {RepositoryFullName} because the GitHub API returned {StatusCode}.",
                resourceName,
                repositoryFullName,
                ex.StatusCode);
            return [];
        }
    }

    private static bool IsSkippableGitHubStatus(HttpStatusCode? statusCode)
        => statusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Gone;

    private async Task<IReadOnlyList<LabelConsistencyWarningDto>> BuildLabelConsistencyWarningsAsync(
        RepositoryReference repositoryReference,
        IReadOnlyList<LabelDto> taxonomyLabels,
        CancellationToken cancellationToken)
    {
        try
        {
            var labels = await _labelRepository
                .GetLabelsAsync(repositoryReference.Owner, repositoryReference.RepoName, cancellationToken)
                .ConfigureAwait(false);

            return LabelConsistencyAnalyser.Analyse(repositoryReference.FullName, labels, taxonomyLabels);
        }
        catch (HttpRequestException ex) when (IsSkippableGitHubStatus(ex.StatusCode))
        {
            _logger.LogWarning(
                ex,
                "Skipping {ResourceName} for {RepositoryFullName} because the GitHub API returned {StatusCode}.",
                "labels",
                repositoryReference.FullName,
                ex.StatusCode);
            return [];
        }
    }

    private async Task<RepositoryAuditSummaryDto> BuildRepositoryAuditSummaryAsync(string owner, string repoName, CancellationToken cancellationToken)
    {
        var repositoryData = await LoadRepositoryAuditDataAsync(owner, repoName, includeWorkflowRuns: true, cancellationToken).ConfigureAwait(false);
        var staleBefore = DateTimeOffset.UtcNow.AddDays(-DefaultStaleDays);
        return BuildRepositoryAuditSummary(repositoryData, staleBefore);
    }

    private static RepositoryAuditSummaryDto BuildRepositoryAuditSummary(RepositoryAuditData repositoryData, DateTimeOffset staleBefore)
    {
        var openIssues = repositoryData.Issues.Where(static issue => IsOpenState(issue.State)).ToArray();
        var openPullRequests = repositoryData.PullRequests.Where(static pullRequest => IsOpenState(pullRequest.State)).ToArray();

        var failingWorkflowCount = GetLatestWorkflowRunsByName(repositoryData.WorkflowRuns)
            .Count(static workflowRun => IsFailingConclusion(workflowRun.Conclusion));

        return new RepositoryAuditSummaryDto(
            repositoryData.RepositoryFullName,
            openIssues.Length,
            openPullRequests.Length,
            openIssues.Count(static issue => issue.Labels.Count == 0),
            openPullRequests.Count(pullRequest => pullRequest.UpdatedAt < staleBefore),
            failingWorkflowCount);
    }

    private static IReadOnlyList<IssueDto> BuildUnlabelledIssues(RepositoryAuditData repositoryData)
        => repositoryData.Issues
            .Where(static issue => IsOpenState(issue.State) && issue.Labels.Count == 0)
            .Select(issue => MapIssue(issue, repositoryData.RepositoryFullName))
            .ToArray();

    private static IReadOnlyList<PullRequestDto> BuildStalePullRequests(RepositoryAuditData repositoryData, DateTimeOffset staleBefore)
        => repositoryData.PullRequests
            .Where(pullRequest => pullRequest.State.Equals("open", StringComparison.OrdinalIgnoreCase) && pullRequest.UpdatedAt < staleBefore)
            .Select(pullRequest => MapPullRequest(pullRequest, repositoryData.RepositoryFullName))
            .ToArray();

    private static IReadOnlyList<WorkflowRunDto> BuildFailingWorkflowRuns(RepositoryAuditData repositoryData)
        => BuildFailingWorkflowRuns(repositoryData.RepositoryFullName, repositoryData.WorkflowRuns);

    private static IReadOnlyList<WorkflowRunDto> BuildFailingWorkflowRuns(string repositoryFullName, IReadOnlyList<WorkflowRun> workflowRuns)
        => GetLatestWorkflowRunsByName(workflowRuns)
            .Where(static workflowRun => IsFailingConclusion(workflowRun.Conclusion))
            .Select(workflowRun => MapWorkflowRun(workflowRun, repositoryFullName))
            .ToArray();

    private static IEnumerable<WorkflowRun> GetLatestWorkflowRunsByName(IReadOnlyList<WorkflowRun> workflowRuns)
        => workflowRuns
            .Where(static workflowRun => !string.IsNullOrWhiteSpace(workflowRun.WorkflowName))
            .GroupBy(static workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
            .Select(static workflowGroup => workflowGroup
                .OrderByDescending(static workflowRun => workflowRun.UpdatedAt)
                .ThenByDescending(static workflowRun => workflowRun.CreatedAt)
                .First());

    private sealed record RepositoryAuditData(
        string RepositoryFullName,
        IReadOnlyList<Issue> Issues,
        IReadOnlyList<PullRequest> PullRequests,
        IReadOnlyList<WorkflowRun> WorkflowRuns);

    private sealed record RepositoryReference(string Owner, string RepoName)
    {
        public string FullName => BuildRepositoryFullName(Owner, RepoName);
    }
}
