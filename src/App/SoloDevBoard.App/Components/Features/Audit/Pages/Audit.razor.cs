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

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

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
    private IReadOnlyList<LabelConsistencyWarningDto> labelConsistencyWarnings = [];
    private IReadOnlyList<string> repositoryOptions = [];
    private HashSet<string> selectedRepositories = new(StringComparer.OrdinalIgnoreCase);
    private int totalOpenIssues;
    private int totalOpenPullRequests;
    private int totalUnlabelledIssues;
    private int totalFailingWorkflows;
    private int totalLabelConsistencyWarnings;
    private bool isLoadingRepositories = true;
    private bool isLoadingAuditData;
    private bool isRefreshingAuditData;
    private bool isLoadingWorkflowHealth;
    private bool isLoadingLabelConsistency;
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
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
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

            // Trigger an immediate render so the refreshing indicator is visible before network calls begin.
            await InvokeAsync(StateHasChanged);
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
            var selectedRepoNames = selectedRepositories
                .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (selectedRepoNames.Length == 0)
            {
                hasLoadedAuditSummary = false;
                ResetDashboardData();
                return;
            }

            var coreSnapshot = await AuditDashboardService.GetDashboardSnapshotAsync(
                selectedRepoNames,
                StalePullRequestDays,
                includeWorkflowRuns: false);

            ApplySnapshot(coreSnapshot);
            hasLoadedAuditSummary = true;
            isLoadingAuditData = false;
            isLoadingWorkflowHealth = true;
            isLoadingLabelConsistency = true;
            await InvokeAsync(StateHasChanged);

            var workflowHealthTask = FetchFailingWorkflowRunsAsync(selectedRepoNames, isBackgroundRefresh);
            var labelConsistencyTask = FetchLabelConsistencyWarningsAsync(selectedRepoNames, isBackgroundRefresh);
            await Task.WhenAll(workflowHealthTask, labelConsistencyTask);

            failingWorkflowRuns = workflowHealthTask.Result;
            labelConsistencyWarnings = labelConsistencyTask.Result;
            ApplySecondaryHealthCountsToSummaries();
            isLoadingWorkflowHealth = false;
            isLoadingLabelConsistency = false;
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories due to a GitHub API error.");
            if (isBackgroundRefresh)
            {
                Snackbar.Add($"Background refresh failed. {ex.Message}", Severity.Warning);
            }
            else
            {
                hasLoadedAuditSummary = false;
                ResetDashboardData();
                auditLoadErrorMessage = $"GitHub API request failed. {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load selected audit repositories.");
            if (isBackgroundRefresh)
            {
                Snackbar.Add("Background refresh failed due to an unexpected error.", Severity.Warning);
            }
            else
            {
                hasLoadedAuditSummary = false;
                ResetDashboardData();
                auditLoadErrorMessage = "An unexpected error occurred while loading the audit summary.";
            }
        }
        finally
        {
            isLoadingAuditData = false;
            isRefreshingAuditData = false;
        }
    }

    private async Task ExportMarkdownSummaryAsync()
    {
        if (!hasLoadedAuditSummary || isLoadingWorkflowHealth || isLoadingLabelConsistency)
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
            labelConsistencyWarnings,
            selectedRepositoryNames,
            totalOpenIssues,
            totalOpenPullRequests,
            totalUnlabelledIssues,
            totalFailingWorkflows,
            totalLabelConsistencyWarnings,
            StalePullRequestDays,
            DateTimeOffset.UtcNow);

    private void ApplySnapshot(AuditDashboardSnapshotDto snapshot)
    {
        repositorySummaries = snapshot.RepositorySummaries
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        unlabelledIssues = snapshot.UnlabelledIssues
            .OrderBy(issue => issue.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Number)
            .ToArray();

        stalePullRequests = snapshot.StalePullRequests
            .OrderBy(pullRequest => pullRequest.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pullRequest => pullRequest.Number)
            .ToArray();

        failingWorkflowRuns = snapshot.FailingWorkflowRuns
            .OrderBy(workflowRun => workflowRun.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        totalOpenIssues = repositorySummaries.Sum(result => result.OpenIssueCount);
        totalOpenPullRequests = repositorySummaries.Sum(result => result.OpenPullRequestCount);
        totalUnlabelledIssues = repositorySummaries.Sum(result => result.UnlabelledIssueCount);
        totalFailingWorkflows = repositorySummaries.Sum(result => result.FailingWorkflowCount);
        totalLabelConsistencyWarnings = repositorySummaries.Sum(result => result.LabelConsistencyWarningCount);
    }

    private async Task<IReadOnlyList<WorkflowRunDto>> FetchFailingWorkflowRunsAsync(IReadOnlyList<string> selectedRepoNames, bool isBackgroundRefresh)
    {
        try
        {
            var failingRuns = await AuditDashboardService.GetFailingWorkflowRunsAsync(selectedRepoNames);
            return failingRuns
                .OrderBy(workflowRun => workflowRun.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(workflowRun => workflowRun.WorkflowName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Failed to load workflow health for selected audit repositories.");
            Snackbar.Add(
                isBackgroundRefresh
                    ? $"Workflow health refresh failed. {ex.Message}"
                    : $"Workflow health could not be loaded. {ex.Message}",
                Severity.Warning);
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load workflow health for selected audit repositories.");
            Snackbar.Add(
                isBackgroundRefresh
                    ? "Workflow health refresh failed due to an unexpected error."
                    : "Workflow health could not be loaded due to an unexpected error.",
                Severity.Warning);
            return [];
        }
    }

    private async Task<IReadOnlyList<LabelConsistencyWarningDto>> FetchLabelConsistencyWarningsAsync(IReadOnlyList<string> selectedRepoNames, bool isBackgroundRefresh)
    {
        try
        {
            var warnings = await AuditDashboardService.GetLabelConsistencyWarningsAsync(selectedRepoNames);
            return warnings
                .OrderBy(warning => warning.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(warning => warning.LabelName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Failed to load label consistency for selected audit repositories.");
            Snackbar.Add(
                isBackgroundRefresh
                    ? $"Label consistency refresh failed. {ex.Message}"
                    : $"Label consistency could not be loaded. {ex.Message}",
                Severity.Warning);
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load label consistency for selected audit repositories.");
            Snackbar.Add(
                isBackgroundRefresh
                    ? "Label consistency refresh failed due to an unexpected error."
                    : "Label consistency could not be loaded due to an unexpected error.",
                Severity.Warning);
            return [];
        }
    }

    private void ApplySecondaryHealthCountsToSummaries()
    {
        var failingWorkflowCountByRepository = failingWorkflowRuns
            .GroupBy(workflowRun => workflowRun.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var warningCountByRepository = labelConsistencyWarnings
            .GroupBy(warning => warning.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        repositorySummaries = repositorySummaries
            .Select(summary => summary with
            {
                FailingWorkflowCount = failingWorkflowCountByRepository.GetValueOrDefault(summary.RepositoryFullName),
                LabelConsistencyWarningCount = warningCountByRepository.GetValueOrDefault(summary.RepositoryFullName),
            })
            .ToArray();

        totalFailingWorkflows = repositorySummaries.Sum(result => result.FailingWorkflowCount);
        totalLabelConsistencyWarnings = repositorySummaries.Sum(result => result.LabelConsistencyWarningCount);
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
        labelConsistencyWarnings = [];
        totalOpenIssues = 0;
        totalOpenPullRequests = 0;
        totalUnlabelledIssues = 0;
        totalFailingWorkflows = 0;
        totalLabelConsistencyWarnings = 0;
    }

    private static string FormatWarningKind(LabelConsistencyWarningKind kind)
        => kind switch
        {
            LabelConsistencyWarningKind.Missing => "Missing",
            LabelConsistencyWarningKind.Divergent => "Divergent",
            _ => kind.ToString(),
        };

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
