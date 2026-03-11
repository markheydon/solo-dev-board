using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays open issue, pull request, and health indicator summaries across repositories.</summary>
public partial class Audit : ComponentBase
{
    private const int StalePullRequestDays = 14;

    /// <summary>Gets or sets the repository service used to load repository options.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

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
    private bool isLoadingRepositories = true;
    private bool isLoadingAuditData;
    private bool hasLoadedAuditSummary;
    private string? repositoryLoadErrorMessage;
    private string? auditLoadErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryOptionsAsync();
    }

    private async Task LoadRepositoryOptionsAsync()
    {
        isLoadingRepositories = true;
        repositoryLoadErrorMessage = null;
        auditLoadErrorMessage = null;

        try
        {
            var repositories = await RepositoryService.GetActiveRepositoriesAsync();

            repositoryOptions = repositories
                .Select(repository => repository.FullName)
                .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedRepositories = [];
            hasLoadedAuditSummary = false;
            ResetDashboardData();
        }
        catch (HttpRequestException ex)
        {
            ResetDashboardData();
            repositoryLoadErrorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load audit dashboard repositories.");
            ResetDashboardData();
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories for the audit dashboard.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private Task OnSelectedRepositoriesChanged(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        selectedRepositories = repositories
            .Where(static repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        auditLoadErrorMessage = null;
        hasLoadedAuditSummary = false;
        ResetDashboardData();
        return Task.CompletedTask;
    }

    private IReadOnlyList<string> selectedRepositoryNames
        => selectedRepositories
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private string RepositoryFilterSummary
        => $"Showing {selectedRepositories.Count} selected of {repositoryOptions.Count} active repositories.";

    private async Task LoadSelectedRepositoriesAsync()
    {
        auditLoadErrorMessage = null;

        if (selectedRepositories.Count == 0)
        {
            hasLoadedAuditSummary = false;
            ResetDashboardData();
            return;
        }

        isLoadingAuditData = true;
        hasLoadedAuditSummary = false;
        ResetDashboardData();

        // Trigger an immediate render so the loading state is visible before network calls begin.
        await InvokeAsync(StateHasChanged);

        try
        {
            await LoadFilteredAuditDataAsync();
            hasLoadedAuditSummary = true;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories due to a GitHub API error.");
            hasLoadedAuditSummary = false;
            ResetDashboardData();
            auditLoadErrorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories.");
            hasLoadedAuditSummary = false;
            ResetDashboardData();
            auditLoadErrorMessage = "An unexpected error occurred while loading the audit summary.";
        }
        finally
        {
            isLoadingAuditData = false;
        }
    }

    private async Task LoadFilteredAuditDataAsync()
    {
        var selectedRepoNames = selectedRepositories
            .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedRepoNames.Length == 0)
        {
            ResetDashboardData();
            return;
        }

        var summaryTask = AuditDashboardService.GetAuditSummaryAsync(selectedRepoNames);

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
