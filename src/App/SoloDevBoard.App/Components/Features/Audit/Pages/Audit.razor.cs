using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Audit;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.Audit.Pages;

/// <summary>Displays open issue, pull request, and health indicator summaries across repositories.</summary>
public partial class Audit : ComponentBase, IAsyncDisposable
{
    private const int StalePullRequestDays = 14;
    private const int DefaultAutoRefreshIntervalMinutes = 5;

    /// <summary>Gets or sets the repository service used to load repository options.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the audit dashboard application service.</summary>
    [Inject]
    public IAuditDashboardService AuditDashboardService { get; set; } = default!;

    /// <summary>Gets or sets the Markdown exporter for audit dashboard snapshots.</summary>
    [Inject]
    public IAuditDashboardMarkdownExporter MarkdownExporter { get; set; } = default!;

    /// <summary>Gets or sets the logger for audit page diagnostics.</summary>
    [Inject]
    public ILogger<Audit> Logger { get; set; } = default!;

    /// <summary>Gets or sets the hosted authentication recovery service.</summary>
    [Inject]
    public IHostedAuthenticationRecoveryService HostedAuthRecovery { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service for user feedback.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Gets or sets the JavaScript runtime used for clipboard export.</summary>
    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    private IReadOnlyList<RepositoryAuditSummaryDto> repositorySummaries = [];
    private IReadOnlyList<IssueDto> unlabelledIssues = [];
    private IReadOnlyList<PullRequestDto> stalePullRequests = [];
    private IReadOnlyList<WorkflowRunDto> failingWorkflowRuns = [];
    private IReadOnlyList<string> repositoryOptions = [];
    private HashSet<string> selectedRepositories = new(StringComparer.OrdinalIgnoreCase);
    private int totalOpenIssues;
    private int totalOpenPullRequests;
    private int totalUnlabelledIssues;
    private int totalFailingWorkflows;
    private bool isLoadingRepositories = true;
    private bool isLoadingAuditData;
    private bool isRefreshingAuditData;
    private bool hasLoadedAuditSummary;
    private string? repositoryLoadErrorMessage;
    private string? auditLoadErrorMessage;
    private int selectedAutoRefreshIntervalMinutes = DefaultAutoRefreshIntervalMinutes;
    private PeriodicTimer? autoRefreshTimer;
    private CancellationTokenSource? autoRefreshCancellationTokenSource;
    private Task? autoRefreshLoopTask;
    private IJSObjectReference? auditClipboardModule;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAutoRefreshAsync();

        if (auditClipboardModule is not null)
        {
            await auditClipboardModule.DisposeAsync();
            auditClipboardModule = null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            auditClipboardModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Features/Audit/Pages/Audit.razor.js");
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryOptionsAsync();
        await StartAutoRefreshAsync();
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

            selectedRepositories.Clear();
            hasLoadedAuditSummary = false;
            ResetDashboardData();
        }
        catch (HostedAuthenticationRequiredException ex)
        {
            if (HostedAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
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

    private Task OnAutoRefreshIntervalChanged(int intervalMinutes)
    {
        selectedAutoRefreshIntervalMinutes = intervalMinutes;
        return StartAutoRefreshAsync();
    }

    private IReadOnlyList<string> selectedRepositoryNames
        => selectedRepositories
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private string RepositoryFilterSummary
        => $"Showing {selectedRepositories.Count} selected of {repositoryOptions.Count} active repositories.";

    private async Task LoadSelectedRepositoriesAsync()
    {
        await LoadAuditDataAsync(isBackgroundRefresh: false);
    }

    private async Task LoadAuditDataAsync(bool isBackgroundRefresh)
    {
        auditLoadErrorMessage = null;

        if (selectedRepositories.Count == 0)
        {
            hasLoadedAuditSummary = false;
            ResetDashboardData();
            return;
        }

        if (isBackgroundRefresh)
        {
            if (!hasLoadedAuditSummary || isLoadingAuditData || isRefreshingAuditData)
            {
                return;
            }

            isRefreshingAuditData = true;
        }
        else
        {
            isLoadingAuditData = true;
            hasLoadedAuditSummary = false;
            ResetDashboardData();

            // Trigger an immediate render so the loading state is visible before network calls begin.
            await InvokeAsync(StateHasChanged);
        }

        try
        {
            await LoadFilteredAuditDataAsync();
            hasLoadedAuditSummary = true;
        }
        catch (HostedAuthenticationRequiredException ex)
        {
            if (HostedAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories due to a GitHub API error.");
            if (!isBackgroundRefresh)
            {
                hasLoadedAuditSummary = false;
                ResetDashboardData();
            }

            auditLoadErrorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories.");
            if (!isBackgroundRefresh)
            {
                hasLoadedAuditSummary = false;
                ResetDashboardData();
            }

            auditLoadErrorMessage = "An unexpected error occurred while loading the audit summary.";
        }
        finally
        {
            isLoadingAuditData = false;
            isRefreshingAuditData = false;
        }
    }

    private async Task ExportMarkdownSummaryAsync()
    {
        if (!hasLoadedAuditSummary)
        {
            Snackbar.Add("Load an audit summary before exporting Markdown.", Severity.Warning);
            return;
        }

        if (auditClipboardModule is null)
        {
            Snackbar.Add("Clipboard export is not ready yet. Please try again.", Severity.Warning);
            return;
        }

        try
        {
            var markdown = MarkdownExporter.GenerateSummaryMarkdown(CreateMarkdownExportRequest());
            await auditClipboardModule.InvokeVoidAsync("copyTextToClipboard", markdown);
            Snackbar.Add("Audit summary copied to clipboard as Markdown.", Severity.Success);
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Failed to copy audit summary Markdown to the clipboard.");
            Snackbar.Add("Could not copy the audit summary to the clipboard.", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to export audit summary as Markdown.");
            Snackbar.Add("An unexpected error occurred while exporting the audit summary.", Severity.Error);
        }
    }

    private AuditDashboardMarkdownExportRequest CreateMarkdownExportRequest()
        => new(
            repositorySummaries,
            unlabelledIssues,
            stalePullRequests,
            failingWorkflowRuns,
            selectedRepositoryNames,
            totalOpenIssues,
            totalOpenPullRequests,
            totalUnlabelledIssues,
            totalFailingWorkflows,
            StalePullRequestDays,
            DateTimeOffset.UtcNow);

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
        totalUnlabelledIssues = repositorySummaries.Sum(result => result.UnlabelledIssueCount);
        totalFailingWorkflows = repositorySummaries.Sum(result => result.FailingWorkflowCount);
    }

    private async Task StartAutoRefreshAsync()
    {
        await StopAutoRefreshAsync();

        if (selectedAutoRefreshIntervalMinutes <= 0)
        {
            return;
        }

        autoRefreshCancellationTokenSource = new CancellationTokenSource();
        autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(selectedAutoRefreshIntervalMinutes));
        autoRefreshLoopTask = RunAutoRefreshLoopAsync(autoRefreshCancellationTokenSource.Token);
    }

    private async Task StopAutoRefreshAsync()
    {
        if (autoRefreshCancellationTokenSource is not null)
        {
            await autoRefreshCancellationTokenSource.CancelAsync();
            autoRefreshCancellationTokenSource.Dispose();
            autoRefreshCancellationTokenSource = null;
        }

        if (autoRefreshTimer is not null)
        {
            autoRefreshTimer.Dispose();
            autoRefreshTimer = null;
        }

        if (autoRefreshLoopTask is not null)
        {
            try
            {
                await autoRefreshLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the component is disposed or the interval changes.
            }

            autoRefreshLoopTask = null;
        }
    }

    private async Task RunAutoRefreshLoopAsync(CancellationToken cancellationToken)
    {
        if (autoRefreshTimer is null)
        {
            return;
        }

        try
        {
            while (await autoRefreshTimer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(async () => await LoadAuditDataAsync(isBackgroundRefresh: true));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the component is disposed or the interval changes.
        }
    }

    private void ResetDashboardData()
    {
        repositorySummaries = [];
        unlabelledIssues = [];
        stalePullRequests = [];
        failingWorkflowRuns = [];
        totalOpenIssues = 0;
        totalOpenPullRequests = 0;
        totalUnlabelledIssues = 0;
        totalFailingWorkflows = 0;
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
