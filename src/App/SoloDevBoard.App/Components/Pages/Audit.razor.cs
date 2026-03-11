using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays open issue, pull request, and health indicator summaries across repositories.</summary>
public partial class Audit : ComponentBase
{
    private const int StalePullRequestDays = 14;

    /// <summary>Gets or sets the audit dashboard application service.</summary>
    [Inject]
    public IAuditDashboardService AuditDashboardService { get; set; } = default!;

    /// <summary>Gets or sets the logger for audit page diagnostics.</summary>
    [Inject]
    public ILogger<Audit> Logger { get; set; } = default!;

    private IReadOnlyList<RepositoryAuditSummaryDto> repositorySummaries = [];
    private IReadOnlyList<IssueDto> unlabelledIssues = [];
    private IReadOnlyList<PullRequestDto> stalePullRequests = [];
    private IReadOnlyList<WorkflowRunDto> failingWorkflowRuns = [];
    private IReadOnlyList<string> repositoryOptions = [];
    private HashSet<string> selectedRepositories = new(StringComparer.OrdinalIgnoreCase);
    private int totalOpenIssues;
    private int totalOpenPullRequests;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadAuditSummaryAsync();
    }

    private async Task LoadAuditSummaryAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var summary = await AuditDashboardService.GetRepositorySummaryAsync();
            var orderedSummary = summary
                .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            repositoryOptions = orderedSummary
                .Select(summaryItem => summaryItem.RepositoryFullName)
                .ToArray();

            selectedRepositories = repositoryOptions
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            await LoadFilteredAuditDataAsync(orderedSummary);
        }
        catch (HttpRequestException ex)
        {
            ResetDashboardData();
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load audit dashboard summary.");
            ResetDashboardData();
            errorMessage = "An unexpected error occurred while loading the audit dashboard.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnSelectedRepositoriesChanged(IEnumerable<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        selectedRepositories = repositories
            .Where(static repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await LoadFilteredAuditDataAsync();
    }

    private async Task LoadFilteredAuditDataAsync(IReadOnlyList<RepositoryAuditSummaryDto>? preloadedSummary = null)
    {
        var selectedRepoNames = selectedRepositories
            .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedRepoNames.Length == 0)
        {
            ResetDashboardData();
            return;
        }

        var summaryTask = preloadedSummary is null
            ? AuditDashboardService.GetAuditSummaryAsync(selectedRepoNames)
            : Task.FromResult(preloadedSummary);

        var unlabelledIssuesTask = AuditDashboardService.GetUnlabelledIssuesAsync(selectedRepoNames);
        var stalePullRequestsTask = AuditDashboardService.GetStalePullRequestsAsync(selectedRepoNames, StalePullRequestDays);
        var failingWorkflowRunsTask = AuditDashboardService.GetFailingWorkflowRunsAsync(selectedRepoNames);

        await Task.WhenAll(summaryTask, unlabelledIssuesTask, stalePullRequestsTask, failingWorkflowRunsTask);

        repositorySummaries = (await summaryTask)
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        unlabelledIssues = (await unlabelledIssuesTask)
            .OrderBy(issue => issue.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Number)
            .ToArray();

        stalePullRequests = (await stalePullRequestsTask)
            .OrderBy(pullRequest => pullRequest.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pullRequest => pullRequest.Number)
            .ToArray();

        failingWorkflowRuns = (await failingWorkflowRunsTask)
            .OrderBy(workflowRun => workflowRun.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        totalOpenIssues = repositorySummaries.Sum(result => result.OpenIssueCount);
        totalOpenPullRequests = repositorySummaries.Sum(result => result.OpenPullRequestCount);
    }

    private void ResetDashboardData()
    {
        repositorySummaries = [];
        unlabelledIssues = [];
        stalePullRequests = [];
        failingWorkflowRuns = [];
        totalOpenIssues = 0;
        totalOpenPullRequests = 0;
    }

    private static int GetDaysBetween(DateTimeOffset value)
    {
        var days = (int)Math.Floor((DateTimeOffset.UtcNow - value).TotalDays);
        return Math.Max(days, 0);
    }

    private static string BuildRepositoryUrl(string repositoryFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        return $"https://github.com/{repositoryFullName}";
    }
}
