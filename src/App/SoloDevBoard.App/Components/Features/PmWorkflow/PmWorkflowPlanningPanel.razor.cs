using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Iteration Planning panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
public partial class PmWorkflowPlanningPanel : ComponentBase, IDisposable
{
    private const string PlanningLoadingAriaLabel = "Loading Iteration Planning";

    [CascadingParameter]
    public PmWorkflowChromeState? ChromeState { get; set; }

    [CascadingParameter(Name = "PmWorkflowDataRevision")]
    public int DataRevision { get; set; }

    [Inject]
    public PmWorkflowChromeCoordinator ChromeCoordinator { get; set; } = default!;

    [Inject]
    public IIterationPlanningService PlanningService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowPlanningPanel> Logger { get; set; } = default!;

    private IterationPlanningViewDto? planningView;
    private bool isLoadingView;
    private bool isAddingToUpNext;
    private string? loadErrorMessage;
    private string candidateSearch = string.Empty;
    private bool showIssues = true;
    private bool showPullRequests = true;
    private string? addingCandidateKey;
    private int loadedDataRevision = -1;
    private int viewLoadGeneration;
    private CancellationTokenSource? viewLoadCts;
    private CancellationTokenSource? addToUpNextCts;

    private IEnumerable<IterationPlanningCandidateDto> FilteredCandidates
    {
        get
        {
            if (planningView is null)
            {
                return [];
            }

            IEnumerable<IterationPlanningCandidateDto> candidates = planningView.Candidates;

            if (!showIssues)
            {
                candidates = candidates.Where(static candidate => candidate.ItemType != PmWorkItemTypeDto.Issue);
            }

            if (!showPullRequests)
            {
                candidates = candidates.Where(static candidate => candidate.ItemType != PmWorkItemTypeDto.PullRequest);
            }

            if (string.IsNullOrWhiteSpace(candidateSearch))
            {
                return candidates;
            }

            var filter = candidateSearch.Trim();
            return candidates.Where(candidate =>
                candidate.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || candidate.RepositoryFullName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || candidate.Number.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        if (ChromeState is null || ChromeState.IsLoading)
        {
            return Task.CompletedTask;
        }

        if (!ChromeState.HasPlanningBoardSelected)
        {
            planningView = null;
            loadErrorMessage = null;
            loadedDataRevision = DataRevision;
            return Task.CompletedTask;
        }

        if (loadedDataRevision == DataRevision && planningView is not null)
        {
            return Task.CompletedTask;
        }

        return LoadPlanningViewAsync();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        viewLoadCts?.Cancel();
        viewLoadCts?.Dispose();
        addToUpNextCts?.Cancel();
        addToUpNextCts?.Dispose();
    }

    private Task RetryLoadAsync() => LoadPlanningViewAsync(forceReload: true);

    private async Task LoadPlanningViewAsync(bool forceReload = false)
    {
        if (ChromeState is null || !ChromeState.HasPlanningBoardSelected)
        {
            return;
        }

        if (!forceReload && loadedDataRevision == DataRevision && planningView is not null)
        {
            return;
        }

        viewLoadCts?.Cancel();
        viewLoadCts?.Dispose();
        viewLoadCts = new CancellationTokenSource();
        var cancellationToken = viewLoadCts.Token;
        var loadGeneration = ++viewLoadGeneration;
        var boardId = ChromeState.Settings.PlanningBoardNodeId!;

        isLoadingView = true;
        loadErrorMessage = null;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            var view = await PlanningService
                .GetPlanningViewAsync(boardId, cancellationToken)
                .ConfigureAwait(false);

            if (loadGeneration != viewLoadGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            planningView = view;
            loadedDataRevision = DataRevision;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            if (loadGeneration != viewLoadGeneration)
            {
                return;
            }

            Logger.LogError(ex, "Failed to load Iteration Planning view for board {BoardId}.", boardId);
            planningView = null;
            loadErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            if (loadGeneration != viewLoadGeneration)
            {
                return;
            }

            Logger.LogError(ex, "Unexpected error while loading Iteration Planning for board {BoardId}.", boardId);
            planningView = null;
            loadErrorMessage = "An unexpected error occurred while loading Iteration Planning.";
        }
        finally
        {
            if (loadGeneration == viewLoadGeneration)
            {
                isLoadingView = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }
    }

    private async Task AddToUpNextAsync(IterationPlanningCandidateDto candidate)
    {
        if (ChromeState is null || !ChromeState.HasPlanningBoardSelected || isAddingToUpNext)
        {
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        var candidateKey = BuildCandidateKey(candidate);
        addToUpNextCts?.Cancel();
        addToUpNextCts?.Dispose();
        addToUpNextCts = new CancellationTokenSource();
        var cancellationToken = addToUpNextCts.Token;

        isAddingToUpNext = true;
        addingCandidateKey = candidateKey;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            await PlanningService
                .AddToUpNextAsync(
                    boardId,
                    candidate.ItemType,
                    candidate.RepositoryFullName,
                    candidate.Number,
                    candidate.Labels,
                    cancellationToken)
                .ConfigureAwait(false);

            Snackbar.Add(
                $"Added {FormatItemReference(candidate.RepositoryFullName, candidate.Number)} to Up Next.",
                Severity.Success,
                configure: config => config.VisibleStateDuration = 4000);

            ChromeCoordinator.ClearDailyFocusBoardState();
            ChromeCoordinator.ClearDailyFocusRecommendations();
            ChromeCoordinator.ClearBacklogReview();
            ChromeState.MarkDataChanged();

            loadedDataRevision = -1;
            await LoadPlanningViewAsync(forceReload: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
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
            Logger.LogError(ex, "GitHub API request failed while adding {CandidateKey} to Up Next.", candidateKey);
            Snackbar.Add(
                $"GitHub API request failed while adding to Up Next. {ex.Message}",
                Severity.Error,
                configure: config => config.VisibleStateDuration = 6000);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Unable to add {CandidateKey} to Up Next.", candidateKey);
            Snackbar.Add(ex.Message, Severity.Error, configure: config => config.VisibleStateDuration = 6000);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while adding {CandidateKey} to Up Next.", candidateKey);
            Snackbar.Add(
                "An unexpected error occurred while adding this item to Up Next.",
                Severity.Error,
                configure: config => config.VisibleStateDuration = 6000);
        }
        finally
        {
            isAddingToUpNext = false;
            addingCandidateKey = null;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private bool IsAddingCandidate(IterationPlanningCandidateDto candidate) =>
        isAddingToUpNext && addingCandidateKey == BuildCandidateKey(candidate);

    private static string BuildCandidateKey(IterationPlanningCandidateDto candidate) =>
        PmWorkItemJoinKey.For(
            candidate.ItemType == PmWorkItemTypeDto.PullRequest,
            candidate.RepositoryFullName,
            candidate.Number);

    private static string FormatItemReference(string repositoryFullName, int number) =>
        $"{repositoryFullName}#{number}";

    private static string FormatItemType(PmWorkItemTypeDto itemType) =>
        itemType == PmWorkItemTypeDto.PullRequest ? "PR" : "Issue";

    private static string FormatFocusOrder(double? focusOrder) =>
        focusOrder?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatPartialFailureMessage(IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return $"Loaded candidates from included repositories, but {failures.Count} {noun} failed: {repositories}.";
    }
}
