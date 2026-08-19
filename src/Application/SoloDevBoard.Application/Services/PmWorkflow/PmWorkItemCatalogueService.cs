using System.Net;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.PmWorkflow;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Loads open issues and pull requests across included active repositories for PM views.</summary>
public sealed class PmWorkItemCatalogueService(
    IGitHubService gitHubService,
    IPmSettingsService pmSettingsService,
    ILogger<PmWorkItemCatalogueService> logger) : IPmWorkItemCatalogueService
{
    private const string OpenItemState = "open";

    /// <inheritdoc/>
    /// <remarks>
    /// A 404 or 410 from GitHub when loading issues or pull requests is treated as an empty list.
    /// Other HTTP failures are recorded on the result so Daily Focus can warn or retry.
    /// </remarks>
    public async Task<PmWorkItemCatalogueResultDto> GetCatalogueAsync(CancellationToken cancellationToken = default)
    {
        var settings = await pmSettingsService.GetSettingsAsync().ConfigureAwait(false);
        var excludedRepositories = new HashSet<string>(
            settings.ExcludedRepositories,
            StringComparer.OrdinalIgnoreCase);

        var repositories = await gitHubService.GetActiveRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        var includedRepositories = repositories
            .Where(repository => !excludedRepositories.Contains(repository.FullName))
            .ToArray();

        var loadTasks = includedRepositories.Select(repository =>
            LoadRepositoryCatalogueAsync(repository, cancellationToken));
        var loadResults = await Task.WhenAll(loadTasks).ConfigureAwait(false);

        var items = loadResults.SelectMany(static result => result.Items).ToArray();
        var failures = loadResults
            .Select(static result => result.Failure)
            .Where(static failure => failure is not null)
            .Select(static failure => failure!)
            .ToArray();
        var failedRepositories = new HashSet<string>(
            failures.Select(static failure => failure.RepositoryFullName),
            StringComparer.OrdinalIgnoreCase);
        var summarisedRepositories = includedRepositories
            .Where(repository => !failedRepositories.Contains(repository.FullName))
            .ToArray();
        var summaries = BuildRepositorySummaries(summarisedRepositories, items);

        return new PmWorkItemCatalogueResultDto(items, failures, summaries);
    }

    private async Task<RepositoryCatalogueLoadResult> LoadRepositoryCatalogueAsync(
        Repository repository,
        CancellationToken cancellationToken)
    {
        var owner = ParseOwnerLogin(repository.FullName);
        var repoName = repository.Name;
        var repositoryFullName = repository.FullName;

        var issueResult = await TryLoadIssuesAsync(owner, repoName, cancellationToken).ConfigureAwait(false);
        var pullRequestResult = await TryLoadPullRequestsAsync(owner, repoName, cancellationToken).ConfigureAwait(false);

        PmRepositoryCatalogueFailureDto? failure = null;
        if (issueResult.ErrorMessage is not null || pullRequestResult.ErrorMessage is not null)
        {
            failure = new PmRepositoryCatalogueFailureDto(
                repositoryFullName,
                CombineFailureMessages(issueResult.ErrorMessage, pullRequestResult.ErrorMessage),
                issueResult.HttpStatusCode ?? pullRequestResult.HttpStatusCode);
        }

        var reviewMetadata = await TryLoadReviewMetadataAsync(owner, repoName, repositoryFullName, cancellationToken)
            .ConfigureAwait(false);
        var reviewMetadataByNumber = reviewMetadata.ToDictionary(static metadata => metadata.Number);

        var epicFeatureIssueNumbers = issueResult.Items
            .Where(static issue => IsOpenState(issue.State))
            .Select(issue => (Issue: issue, Labels: issue.Labels.Select(static label => label.Name).ToArray()))
            .Where(static pair => IsEpicOrFeature(pair.Labels))
            .Select(pair => pair.Issue.Number)
            .Distinct()
            .ToArray();

        var subIssueSummaries = await TryLoadSubIssueSummariesAsync(
                owner,
                repoName,
                repositoryFullName,
                epicFeatureIssueNumbers,
                cancellationToken)
            .ConfigureAwait(false);
        var subIssueSummaryByNumber = subIssueSummaries.ToDictionary(static summary => summary.Number);

        var items = new List<PmWorkItemDto>(
            issueResult.Items.Count(static issue => IsOpenState(issue.State))
            + pullRequestResult.Items.Count(static pullRequest => IsOpenState(pullRequest.State)));

        foreach (var issue in issueResult.Items.Where(static issue => IsOpenState(issue.State)))
        {
            var labels = issue.Labels.Select(static label => label.Name).ToArray();
            subIssueSummaryByNumber.TryGetValue(issue.Number, out var subIssueSummary);

            items.Add(MapIssue(
                issue,
                repositoryFullName,
                labels,
                subIssueSummary));
        }

        foreach (var pullRequest in pullRequestResult.Items.Where(static pullRequest => IsOpenState(pullRequest.State)))
        {
            reviewMetadataByNumber.TryGetValue(pullRequest.Number, out var reviewMetadataEntry);
            var labels = pullRequest.Labels.Select(static label => label.Name).ToArray();

            items.Add(MapPullRequest(
                pullRequest,
                repositoryFullName,
                labels,
                reviewMetadataEntry));
        }

        return new RepositoryCatalogueLoadResult(items, failure);
    }

    private async Task<LoadAttempt<IReadOnlyList<Issue>>> TryLoadIssuesAsync(
        string owner,
        string repoName,
        CancellationToken cancellationToken)
    {
        try
        {
            var issues = await gitHubService
                .GetIssuesAsync(owner, repoName, OpenItemState, cancellationToken)
                .ConfigureAwait(false);
            return new LoadAttempt<IReadOnlyList<Issue>>(issues, null, null);
        }
        catch (HttpRequestException exception) when (IsAbsentGitHubResource(exception.StatusCode))
        {
            // Do not attach the exception. Aspire surfaces exception payloads as errors even at Warning level.
            logger.LogWarning(
                "Treating issues for {RepositoryFullName} as empty because the GitHub API returned {StatusCode}.",
                $"{owner}/{repoName}",
                exception.StatusCode);
            return new LoadAttempt<IReadOnlyList<Issue>>([], null, null);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to load issues for {RepositoryFullName} while building the PM work-item catalogue.",
                $"{owner}/{repoName}");
            return new LoadAttempt<IReadOnlyList<Issue>>([], exception.Message, (int?)exception.StatusCode);
        }
    }

    private async Task<LoadAttempt<IReadOnlyList<PullRequest>>> TryLoadPullRequestsAsync(
        string owner,
        string repoName,
        CancellationToken cancellationToken)
    {
        try
        {
            var pullRequests = await gitHubService
                .GetPullRequestsAsync(owner, repoName, OpenItemState, cancellationToken)
                .ConfigureAwait(false);
            return new LoadAttempt<IReadOnlyList<PullRequest>>(pullRequests, null, null);
        }
        catch (HttpRequestException exception) when (IsAbsentGitHubResource(exception.StatusCode))
        {
            // Do not attach the exception. Aspire surfaces exception payloads as errors even at Warning level.
            logger.LogWarning(
                "Treating pull requests for {RepositoryFullName} as empty because the GitHub API returned {StatusCode}.",
                $"{owner}/{repoName}",
                exception.StatusCode);
            return new LoadAttempt<IReadOnlyList<PullRequest>>([], null, null);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to load pull requests for {RepositoryFullName} while building the PM work-item catalogue.",
                $"{owner}/{repoName}");
            return new LoadAttempt<IReadOnlyList<PullRequest>>([], exception.Message, (int?)exception.StatusCode);
        }
    }

    private async Task<IReadOnlyList<PullRequestReviewMetadata>> TryLoadReviewMetadataAsync(
        string owner,
        string repoName,
        string repositoryFullName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await gitHubService
                .GetOpenPullRequestReviewMetadataAsync(owner, repoName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to load pull request review metadata for {RepositoryFullName}; continuing without review-pending signals.",
                repositoryFullName);
            return [];
        }
    }

    private async Task<IReadOnlyList<IssueSubIssueSummary>> TryLoadSubIssueSummariesAsync(
        string owner,
        string repoName,
        string repositoryFullName,
        IReadOnlyList<int> issueNumbers,
        CancellationToken cancellationToken)
    {
        if (issueNumbers.Count == 0)
        {
            return [];
        }

        try
        {
            return await gitHubService
                .GetIssueSubIssueSummariesAsync(owner, repoName, issueNumbers, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to load sub-issue summaries for {RepositoryFullName}; continuing without epic completion counts.",
                repositoryFullName);
            return [];
        }
    }

    private static PmWorkItemDto MapIssue(
        Issue issue,
        string repositoryFullName,
        IReadOnlyList<string> labels,
        IssueSubIssueSummary? subIssueSummary)
        => new(
            PmWorkItemTypeDto.Issue,
            issue.Number,
            issue.Title,
            issue.HtmlUrl,
            repositoryFullName,
            labels,
            issue.Milestone?.Number,
            issue.Milestone?.Title,
            issue.CreatedAt,
            issue.UpdatedAt,
            IsDraft: null,
            HasReviewPending: null,
            SubIssueTotal: subIssueSummary?.TotalCount,
            SubIssueCompleted: subIssueSummary?.CompletedCount);

    private static PmWorkItemDto MapPullRequest(
        PullRequest pullRequest,
        string repositoryFullName,
        IReadOnlyList<string> labels,
        PullRequestReviewMetadata? reviewMetadata)
        => new(
            PmWorkItemTypeDto.PullRequest,
            pullRequest.Number,
            pullRequest.Title,
            pullRequest.HtmlUrl,
            repositoryFullName,
            labels,
            pullRequest.Milestone?.Number,
            pullRequest.Milestone?.Title,
            pullRequest.CreatedAt,
            pullRequest.UpdatedAt,
            pullRequest.IsDraft,
            reviewMetadata?.HasReviewPending,
            SubIssueTotal: null,
            SubIssueCompleted: null);

    private static IReadOnlyList<PmRepositorySummaryDto> BuildRepositorySummaries(
        IReadOnlyList<Repository> includedRepositories,
        IReadOnlyList<PmWorkItemDto> items)
    {
        var itemsByRepository = items.ToLookup(
            static item => item.RepositoryFullName,
            StringComparer.OrdinalIgnoreCase);

        return includedRepositories
            .OrderBy(static repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(repository =>
            {
                var repositoryItems = itemsByRepository[repository.FullName].ToArray();
                var lastActivity = repositoryItems.Length == 0
                    ? repository.UpdatedAt
                    : repositoryItems.Max(static item => item.UpdatedAt);

                return new PmRepositorySummaryDto(
                    repository.FullName,
                    repositoryItems.Count(static item => item.ItemType == PmWorkItemTypeDto.Issue),
                    repositoryItems.Count(static item => item.ItemType == PmWorkItemTypeDto.PullRequest),
                    lastActivity,
                    IsIncluded: true);
            })
            .ToArray();
    }

    private static bool IsOpenState(string state)
        => state.Equals(OpenItemState, StringComparison.OrdinalIgnoreCase);

    private static bool IsEpicOrFeature(IReadOnlyList<string> labels)
    {
        var typeLabel = PmLabelHelpers.ParseTypeLabel(labels);
        return typeLabel is PmLabelHelpers.EpicTypeLabel or PmLabelHelpers.FeatureTypeLabel;
    }

    private static string CombineFailureMessages(string? issueError, string? pullRequestError)
    {
        return (issueError, pullRequestError) switch
        {
            (null, null) => string.Empty,
            (not null, null) => $"Issues: {issueError}",
            (null, not null) => $"Pull requests: {pullRequestError}",
            (not null, not null) => $"Issues: {issueError}; Pull requests: {pullRequestError}",
        };
    }

    /// <summary>
    /// Returns whether a GitHub status means the issues or pull-requests resource is unavailable
    /// for a repository that was already listed, rather than a load failure to surface in the UI.
    /// </summary>
    /// <param name="statusCode">The status from a GitHub <see cref="HttpRequestException"/>.</param>
    /// <returns>
    /// <see langword="true"/> for <see cref="HttpStatusCode.NotFound"/> and <see cref="HttpStatusCode.Gone"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Profile README repositories such as <c>owner/owner</c> often return 404 from
    /// <c>GET /repos/{owner}/{repo}/pulls</c> while listing the repository and its issues still succeeds.
    /// The Audit Dashboard uses the same skip for those statuses.
    /// </remarks>
    private static bool IsAbsentGitHubResource(HttpStatusCode? statusCode)
        => statusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;

    private static string ParseOwnerLogin(string repositoryFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);

        var separatorIndex = repositoryFullName.IndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= repositoryFullName.Length - 1)
        {
            throw new ArgumentException("Repository full name must be in owner/name form.", nameof(repositoryFullName));
        }

        return repositoryFullName[..separatorIndex];
    }

    private sealed record LoadAttempt<T>(T Items, string? ErrorMessage, int? HttpStatusCode);

    private sealed record RepositoryCatalogueLoadResult(
        IReadOnlyList<PmWorkItemDto> Items,
        PmRepositoryCatalogueFailureDto? Failure);
}
