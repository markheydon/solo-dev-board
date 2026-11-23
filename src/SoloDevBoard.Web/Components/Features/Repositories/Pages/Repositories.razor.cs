using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Web.Authentication;
namespace SoloDevBoard.Web.Components.Features.Repositories.Pages;

/// <summary>Displays repositories available to the authenticated GitHub account.</summary>
public partial class Repositories : ComponentBase, IAsyncDisposable
{
    private static readonly TimeSpan SnapshotUiPollInterval = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the operator snapshot collection service.</summary>
    [Inject]
    public IOperatorSnapshotCollectionService SnapshotCollectionService { get; set; } = default!;

    /// <summary>Gets or sets the logger for repository page diagnostics.</summary>
    [Inject]
    public ILogger<Repositories> Logger { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service used for transient page notifications.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> repositories = [];
    private bool isLoading = true;
    private string? errorMessage;
    private string feedbackMessage = "Loading repositories...";
    private Severity feedbackSeverity = Severity.Info;
    private string? catalogueFeedbackLine;
    private OperatorSnapshotStatusDto? snapshotStatus;
    private bool awaitingInitialSnapshotStatus = true;
    private bool snapshotStatusUnavailable;
    private string? repositorySearchTerm;
    private RepositoryCatalogueFilter catalogueFilter = RepositoryCatalogueFilter.All;
    private PeriodicTimer? statusPollTimer;
    private CancellationTokenSource? statusPollCts;

    private bool IsCollectSnapshotDisabled =>
        isLoading
        || awaitingInitialSnapshotStatus
        || snapshotStatus?.Kind == OperatorSnapshotStatusKind.InProgress
        || !string.IsNullOrEmpty(snapshotStatus?.QueuedForOccasion);

    private IReadOnlyList<RepositoryDto> FilteredRepositories
    {
        get
        {
            var filtered = RepositoryCatalogueFilters.Apply(repositories, catalogueFilter);

            if (string.IsNullOrWhiteSpace(repositorySearchTerm))
            {
                return filtered;
            }

            return filtered
                .Where(repository =>
                    repository.Name.Contains(repositorySearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    repository.FullName.Contains(repositorySearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopStatusPollingAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshSnapshotStatusAsync();
        await LoadRepositoriesAsync();
        ApplyCombinedFeedback();
        StateHasChanged();
        await EnsureStatusPollingAsync();
    }

    private async Task ReloadAsync()
    {
        ShowTransientFeedback("Reloading repositories from GitHub.", Severity.Info);
        await LoadRepositoriesAsync(forceReload: true);
        await RefreshSnapshotStatusOnUiAsync();
    }

    private async Task CollectSnapshotAsync()
    {
        try
        {
            var start = await SnapshotCollectionService.StartAsync();
            if (start == OperatorSnapshotStartResult.UnsafeOperatorLogin)
            {
                var catalogueLine = catalogueFeedbackLine ?? feedbackMessage;
                feedbackMessage =
                    $"{catalogueLine.TrimEnd()} Snapshot could not start because your sign-in could not be stored safely.";
                feedbackSeverity = Severity.Error;
                StateHasChanged();
                return;
            }

            await RefreshSnapshotStatusOnUiAsync();
            await EnsureStatusPollingAsync();
            await WaitForSnapshotSettlementOnUiAsync();
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start operator snapshot collection.");
            snapshotStatusUnavailable = true;
            ApplyCombinedFeedback();
            StateHasChanged();
        }
    }

    private void AddRepository()
    {
        ShowPlaceholderFeedback("Add repository will be available in a future milestone.");
    }

    private void RemoveSelectedRepositories()
    {
        ShowPlaceholderFeedback("Remove repositories will be available in a future milestone.");
    }

    private void OpenBulkActions()
    {
        ShowPlaceholderFeedback("Bulk actions will be available in a future milestone.");
    }

    private void EditRepository(RepositoryDto repository)
    {
        ShowPlaceholderFeedback($"Edit repository '{repository.Name}' will be available in a future milestone.");
    }

    private void OpenRepositoryMoreActions(RepositoryDto repository)
    {
        ShowPlaceholderFeedback($"More actions for '{repository.Name}' will be available in a future milestone.");
    }

    private static string GetRepositoryStatusText(RepositoryDto repository)
        => repository.IsArchived ? "Archived" : "Connected";

    private static Color GetRepositoryStatusColour(RepositoryDto repository)
        => repository.IsArchived ? Color.Warning : Color.Success;

    private string GetFilterEmptyMessage()
    {
        if (!string.IsNullOrWhiteSpace(repositorySearchTerm))
        {
            return "No repositories match your search and filter.";
        }

        return catalogueFilter switch
        {
            RepositoryCatalogueFilter.OpenSource =>
                "No catalogue repositories currently have the open-source topic.",
            RepositoryCatalogueFilter.NotOpenSource =>
                "Every catalogue repository currently has the open-source topic.",
            _ => "No repositories match the current filter.",
        };
    }

    private void ShowTransientFeedback(string message, Severity severity)
    {
        feedbackMessage = message;
        feedbackSeverity = severity;
    }

    private void SetCatalogueFeedback(string message, Severity severity)
    {
        catalogueFeedbackLine = message;
        feedbackMessage = message;
        feedbackSeverity = severity;
        ApplyCombinedFeedback();
    }

    private void ApplySnapshotStatus(OperatorSnapshotStatusDto status)
    {
        snapshotStatus = status;
        snapshotStatusUnavailable = false;
        ApplyCombinedFeedback();
    }

    private void ApplyCombinedFeedback()
    {
        if (isLoading || !string.IsNullOrEmpty(errorMessage))
        {
            return;
        }

        var catalogueLine = catalogueFeedbackLine ?? feedbackMessage;
        var suffix = OperatorSnapshotStatusCopy.BuildSnapshotSuffix(snapshotStatus, snapshotStatusUnavailable);
        feedbackMessage = string.IsNullOrEmpty(suffix)
            ? catalogueLine
            : $"{catalogueLine.TrimEnd()} {suffix}";
        var catalogueSeverity = catalogueFeedbackLine is null
            ? feedbackSeverity
            : repositories.Count == 0 ? Severity.Info : Severity.Success;
        feedbackSeverity = OperatorSnapshotStatusCopy.ResolveCombinedSeverity(
            snapshotStatus,
            snapshotStatusUnavailable,
            catalogueSeverity);
    }

    private async Task RefreshSnapshotStatusAsync()
    {
        try
        {
            var status = await SnapshotCollectionService.GetStatusAsync();
            ApplySnapshotStatus(status);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read operator snapshot status.");
            snapshotStatus = null;
            snapshotStatusUnavailable = true;
            ApplyCombinedFeedback();
        }
        finally
        {
            awaitingInitialSnapshotStatus = false;
        }
    }

    private async Task RefreshSnapshotStatusOnUiAsync()
    {
        await RefreshSnapshotStatusAsync();
        StateHasChanged();
    }

    private async Task WaitForSnapshotSettlementOnUiAsync()
    {
        if (snapshotStatus?.Kind != OperatorSnapshotStatusKind.InProgress)
        {
            return;
        }

        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        while (snapshotStatus?.Kind == OperatorSnapshotStatusKind.InProgress && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(SnapshotUiPollInterval);
            await RefreshSnapshotStatusOnUiAsync();
        }

        await StopStatusPollingAsync();
    }

    private async Task EnsureStatusPollingAsync()
    {
        if (snapshotStatus?.Kind != OperatorSnapshotStatusKind.InProgress)
        {
            await StopStatusPollingAsync();
            return;
        }

        if (statusPollTimer is not null)
        {
            return;
        }

        statusPollCts = new CancellationTokenSource();
        statusPollTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = PollStatusLoopAsync(statusPollCts.Token);
    }

    private async Task PollStatusLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                await InvokeAsync(RefreshSnapshotStatusOnUiAsync);
                if (snapshotStatus?.Kind != OperatorSnapshotStatusKind.InProgress)
                {
                    await StopStatusPollingAsync();
                    break;
                }
            }
            while (statusPollTimer is not null
                   && await statusPollTimer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Expected when the component is disposed.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Snapshot status polling failed.");
        }
    }

    private async Task StopStatusPollingAsync()
    {
        if (statusPollCts is not null)
        {
            await statusPollCts.CancelAsync();
            statusPollCts.Dispose();
            statusPollCts = null;
        }

        statusPollTimer?.Dispose();
        statusPollTimer = null;
    }

    private void ShowPlaceholderFeedback(string message)
    {
        Snackbar.Add(message, Severity.Info);
    }

    private async Task LoadRepositoriesAsync(bool forceReload = false)
    {
        isLoading = true;
        errorMessage = null;
        ShowTransientFeedback("Loading repositories...", Severity.Info);

        try
        {
            repositories = await RepositoryService.GetRepositoriesAsync(forceReload: forceReload);
            SetCatalogueFeedback(
                repositories.Count == 0
                    ? "No repositories are connected yet."
                    : $"Loaded {repositories.Count} repositories.",
                repositories.Count == 0 ? Severity.Info : Severity.Success);
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
            errorMessage = $"GitHub API request failed. {ex.Message}";
            ShowTransientFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load repositories.");
            errorMessage = "An unexpected error occurred while loading repositories.";
            ShowTransientFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        finally
        {
            isLoading = false;
            if (string.IsNullOrEmpty(errorMessage))
            {
                ApplyCombinedFeedback();
            }
        }
    }
}
