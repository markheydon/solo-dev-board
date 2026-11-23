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
    private string snapshotStatusMessage = "Loading collection status...";
    private Severity snapshotStatusSeverity = Severity.Info;
    private OperatorSnapshotStatusDto? snapshotStatus;
    private string? repositorySearchTerm;
    private RepositoryCatalogueFilter catalogueFilter = RepositoryCatalogueFilter.All;
    private PeriodicTimer? statusPollTimer;
    private CancellationTokenSource? statusPollCts;

    private bool IsCollectSnapshotDisabled =>
        isLoading || snapshotStatus?.Kind == OperatorSnapshotStatusKind.InProgress;

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
        await StopStatusPollingAsync().ConfigureAwait(false);
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshSnapshotStatusAsync().ConfigureAwait(false);
        await LoadRepositoriesAsync().ConfigureAwait(false);
        await EnsureStatusPollingAsync().ConfigureAwait(false);
    }

    private async Task ReloadAsync()
    {
        SetFeedback("Reloading repositories from GitHub.", Severity.Info);
        await LoadRepositoriesAsync(forceReload: true).ConfigureAwait(false);
        await RefreshSnapshotStatusAsync().ConfigureAwait(false);
    }

    private async Task CollectSnapshotAsync()
    {
        try
        {
            var result = await SnapshotCollectionService.StartAsync().ConfigureAwait(false);
            if (result == OperatorSnapshotStartResult.AlreadyInProgress)
            {
                Snackbar.Add("A collection is already in progress.", Severity.Info);
            }
            else
            {
                Snackbar.Add("Collection started.", Severity.Success);
            }

            await RefreshSnapshotStatusAsync().ConfigureAwait(false);
            await EnsureStatusPollingAsync().ConfigureAwait(false);
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
            Snackbar.Add("Could not start collection.", Severity.Error);
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

    private void SetFeedback(string message, Severity severity)
    {
        feedbackMessage = message;
        feedbackSeverity = severity;
    }

    private void ApplySnapshotStatus(OperatorSnapshotStatusDto status)
    {
        snapshotStatus = status;
        snapshotStatusMessage = OperatorSnapshotStatusCopy.BuildStatusMessage(status);
        snapshotStatusSeverity = status.Kind switch
        {
            OperatorSnapshotStatusKind.Succeeded => Severity.Success,
            OperatorSnapshotStatusKind.Failed => Severity.Error,
            OperatorSnapshotStatusKind.Interrupted => Severity.Warning,
            OperatorSnapshotStatusKind.InProgress => Severity.Info,
            _ => Severity.Normal,
        };
    }

    private async Task RefreshSnapshotStatusAsync()
    {
        try
        {
            var status = await SnapshotCollectionService.GetStatusAsync().ConfigureAwait(false);
            ApplySnapshotStatus(status);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read operator snapshot status.");
            snapshotStatusMessage = "Collection status is unavailable.";
            snapshotStatusSeverity = Severity.Warning;
        }
    }

    private async Task EnsureStatusPollingAsync()
    {
        if (snapshotStatus?.Kind != OperatorSnapshotStatusKind.InProgress)
        {
            await StopStatusPollingAsync().ConfigureAwait(false);
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
            while (statusPollTimer is not null && await statusPollTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await InvokeAsync(async () =>
                {
                    await RefreshSnapshotStatusAsync().ConfigureAwait(false);
                    if (snapshotStatus?.Kind != OperatorSnapshotStatusKind.InProgress)
                    {
                        await StopStatusPollingAsync().ConfigureAwait(false);
                    }

                    StateHasChanged();
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the component is disposed.
        }
    }

    private async Task StopStatusPollingAsync()
    {
        if (statusPollCts is not null)
        {
            await statusPollCts.CancelAsync().ConfigureAwait(false);
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
        SetFeedback("Loading repositories...", Severity.Info);

        try
        {
            repositories = await RepositoryService.GetRepositoriesAsync(forceReload: forceReload);
            SetFeedback(
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
            SetFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load repositories.");
            errorMessage = "An unexpected error occurred while loading repositories.";
            SetFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        finally
        {
            isLoading = false;
        }
    }
}
