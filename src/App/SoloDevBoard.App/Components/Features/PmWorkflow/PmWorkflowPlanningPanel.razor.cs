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

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    private IterationPlanningViewDto? planningView;
    private bool isLoadingView;
    private bool isAddingToUpNext;
    private string? loadErrorMessage;
    private string candidateSearch = string.Empty;
    private string selectedTypeFilter = PmWorkflowItemKindFormatting.AllTypesFilter;
    private string? addingCandidateKey;
    private string? resolvingStalledItemId;
    private int viewLoadGeneration;
    private CancellationTokenSource? viewLoadCts;
    private CancellationTokenSource? addToUpNextCts;
    private CancellationTokenSource? resolveStalledCts;

    private bool IsAddToUpNextDisabled =>
        planningView is not null && planningView.StalledUpNextItems.Count > 0;

    private IEnumerable<IterationPlanningCandidateDto> FilteredCandidates
    {
        get
        {
            if (planningView is null)
            {
                return [];
            }

            IEnumerable<IterationPlanningCandidateDto> candidates = planningView.Candidates;

            candidates = candidates.Where(candidate =>
                PmWorkflowItemKindFormatting.MatchesTypeFilter(candidate.ItemType, selectedTypeFilter));

            if (string.IsNullOrWhiteSpace(candidateSearch))
            {
                return candidates;
            }

            var filter = candidateSearch.Trim();
            return candidates.Where(candidate =>
                candidate.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || candidate.RepositoryFullName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || candidate.Number.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(filter, StringComparison.OrdinalIgnoreCase)
                || $"{candidate.RepositoryFullName}#{candidate.Number}".Contains(filter, StringComparison.OrdinalIgnoreCase));
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
            return Task.CompletedTask;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        if (TryApplyCachedView(boardId))
        {
            return Task.CompletedTask;
        }

        _ = LoadPlanningViewAsync(boardId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        viewLoadCts?.Cancel();
        viewLoadCts?.Dispose();
        addToUpNextCts?.Cancel();
        addToUpNextCts?.Dispose();
        resolveStalledCts?.Cancel();
        resolveStalledCts?.Dispose();
    }

    private Task RetryLoadAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearIterationPlanning();
        return LoadPlanningViewAsync(ChromeState.Settings.PlanningBoardNodeId, forceReload: true);
    }

    private Task OnTypeFilterChanged(string value)
    {
        selectedTypeFilter = value ?? PmWorkflowItemKindFormatting.AllTypesFilter;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnSearchChanged(string value)
    {
        candidateSearch = value ?? string.Empty;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private bool TryApplyCachedView(string boardId)
    {
        var cached = ChromeCoordinator.IterationPlanning;
        if (cached is null || !cached.BoardId.Equals(boardId, StringComparison.Ordinal))
        {
            return false;
        }

        isLoadingView = cached.IsLoading;
        planningView = cached.View;
        loadErrorMessage = cached.ErrorMessage;
        return cached.IsLoading
            || cached.View is not null
            || !string.IsNullOrWhiteSpace(cached.ErrorMessage);
    }

    private async Task LoadPlanningViewAsync(string boardId, bool forceReload = false)
    {
        if (!forceReload && TryApplyCachedView(boardId))
        {
            return;
        }

        viewLoadCts?.Cancel();
        viewLoadCts?.Dispose();
        viewLoadCts = new CancellationTokenSource();
        var cancellationToken = viewLoadCts.Token;
        var loadGeneration = ++viewLoadGeneration;

        isLoadingView = true;
        loadErrorMessage = null;
        planningView = null;
        ChromeCoordinator.SetIterationPlanning(boardId, null, null, isLoading: true);
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            var view = await PlanningService
                .GetPlanningViewAsync(
                    boardId,
                    ChromeState!.Settings.Capacity,
                    ChromeState.Settings.StallDays,
                    cancellationToken)
                .ConfigureAwait(false);

            if (loadGeneration != viewLoadGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            planningView = view;
            ChromeCoordinator.SetIterationPlanning(boardId, view, null, isLoading: false);
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
            ChromeCoordinator.SetIterationPlanning(boardId, null, loadErrorMessage, isLoading: false);
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
            ChromeCoordinator.SetIterationPlanning(boardId, null, loadErrorMessage, isLoading: false);
        }
        finally
        {
            if (loadGeneration == viewLoadGeneration)
            {
                isLoadingView = false;
                var cached = ChromeCoordinator.IterationPlanning;
                if (cached is not null
                    && cached.BoardId.Equals(boardId, StringComparison.Ordinal)
                    && cached.IsLoading)
                {
                    ChromeCoordinator.SetIterationPlanning(
                        boardId,
                        cached.View,
                        cached.ErrorMessage,
                        isLoading: false);
                }

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

        if (planningView is not null
            && PlanningCapacityEvaluator.WouldExceedCapacityAfterAdd(
                planningView.ActiveLoad,
                planningView.Capacity))
        {
            var confirmed = await ConfirmCapacityExceededAsync().ConfigureAwait(false);
            if (!confirmed)
            {
                return;
            }
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
            var result = await PlanningService
                .AddToUpNextAsync(
                    boardId,
                    candidate.ItemType,
                    candidate.RepositoryFullName,
                    candidate.Number,
                    candidate.Labels,
                    cancellationToken)
                .ConfigureAwait(false);

            Snackbar.Add(
                FormatAddToUpNextMessage(candidate, result, planningView?.HasFocusOrderField ?? true),
                Severity.Success,
                configure: config => config.VisibleStateDuration = 5000);

            ChromeCoordinator.ClearDailyFocusBoardState();
            ChromeCoordinator.ClearDailyFocusRecommendations();
            ChromeCoordinator.ClearBacklogReview();
            ChromeCoordinator.ClearIterationPlanning();
            ChromeState.MarkDataChanged();

            await LoadPlanningViewAsync(boardId, forceReload: true).ConfigureAwait(false);
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

    private bool IsResolvingStalledItem(IterationPlanningStalledItemDto item) =>
        !string.IsNullOrWhiteSpace(resolvingStalledItemId)
        && resolvingStalledItemId.Equals(item.ProjectItemId, StringComparison.Ordinal);

    private Task ReCommitStalledItemAsync(IterationPlanningStalledItemDto item) =>
        ResolveStalledItemAsync(
            item,
            (projectId, cancellationToken) =>
                PlanningService.ReCommitStalledUpNextItemAsync(projectId, item.ProjectItemId, cancellationToken),
            $"Re-committed {FormatItemReference(item.RepositoryFullName, item.Number)} to Up Next.");

    private Task MarkStalledItemBlockedAsync(IterationPlanningStalledItemDto item) =>
        ResolveStalledItemAsync(
            item,
            (projectId, cancellationToken) =>
                PlanningService.MarkStalledUpNextItemBlockedAsync(projectId, item, cancellationToken),
            $"Marked {FormatItemReference(item.RepositoryFullName, item.Number)} as blocked.");

    private Task MoveStalledItemToIceBoxAsync(IterationPlanningStalledItemDto item) =>
        ResolveStalledItemAsync(
            item,
            (projectId, cancellationToken) =>
                PlanningService.MoveStalledUpNextItemToIceBoxAsync(projectId, item, cancellationToken),
            $"Moved {FormatItemReference(item.RepositoryFullName, item.Number)} to Ice Box.");

    private Task RemoveStalledItemAsync(IterationPlanningStalledItemDto item) =>
        ResolveStalledItemAsync(
            item,
            (projectId, cancellationToken) =>
                PlanningService.RemoveStalledUpNextItemAsync(projectId, item, cancellationToken),
            $"Removed {FormatItemReference(item.RepositoryFullName, item.Number)} from Up Next.");

    private async Task ResolveStalledItemAsync(
        IterationPlanningStalledItemDto item,
        Func<string, CancellationToken, Task> action,
        string successMessage)
    {
        if (ChromeState is null || !ChromeState.HasPlanningBoardSelected || IsResolvingStalledItem(item))
        {
            return;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        resolveStalledCts?.Cancel();
        resolveStalledCts?.Dispose();
        resolveStalledCts = new CancellationTokenSource();
        var cancellationToken = resolveStalledCts.Token;

        resolvingStalledItemId = item.ProjectItemId;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            await action(boardId, cancellationToken).ConfigureAwait(false);

            Snackbar.Add(successMessage, Severity.Success, configure: config => config.VisibleStateDuration = 5000);
            await ReloadPlanningDataAsync(boardId).ConfigureAwait(false);
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
            Logger.LogError(ex, "GitHub API request failed while resolving stalled item {ProjectItemId}.", item.ProjectItemId);
            Snackbar.Add(
                $"GitHub API request failed while resolving the stalled item. {ex.Message}",
                Severity.Error,
                configure: config => config.VisibleStateDuration = 6000);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Unable to resolve stalled item {ProjectItemId}.", item.ProjectItemId);
            Snackbar.Add(ex.Message, Severity.Error, configure: config => config.VisibleStateDuration = 6000);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while resolving stalled item {ProjectItemId}.", item.ProjectItemId);
            Snackbar.Add(
                "An unexpected error occurred while resolving the stalled item.",
                Severity.Error,
                configure: config => config.VisibleStateDuration = 6000);
        }
        finally
        {
            resolvingStalledItemId = null;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task ReloadPlanningDataAsync(string boardId)
    {
        ChromeCoordinator.ClearDailyFocusBoardState();
        ChromeCoordinator.ClearDailyFocusRecommendations();
        ChromeCoordinator.ClearBacklogReview();
        ChromeCoordinator.ClearIterationPlanning();
        ChromeState?.MarkDataChanged();
        await LoadPlanningViewAsync(boardId, forceReload: true).ConfigureAwait(false);
    }

    private static string BuildCandidateKey(IterationPlanningCandidateDto candidate) =>
        PmWorkItemJoinKey.For(
            candidate.ItemType == PmWorkItemTypeDto.PullRequest,
            candidate.RepositoryFullName,
            candidate.Number);

    private static string FormatItemReference(string repositoryFullName, int number) =>
        $"{repositoryFullName}#{number}";

    private static string FormatFocusOrder(double? focusOrder) =>
        focusOrder?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatAddToUpNextMessage(
        IterationPlanningCandidateDto candidate,
        IterationPlanningAddToUpNextResultDto result,
        bool hasFocusOrderField)
    {
        var itemReference = FormatItemReference(candidate.RepositoryFullName, candidate.Number);

        if (result.FocusOrderAssigned.HasValue)
        {
            return $"Added {itemReference} to Up Next with Focus Order {FormatFocusOrder(result.FocusOrderAssigned)}.";
        }

        if (result.FocusOrderSkipped)
        {
            var skipReason = PlanningFocusOrderSequencer.DescribeFocusOrderSkipReason(
                candidate.Labels,
                hasFocusOrderField)
                ?? "Focus Order was not assigned.";
            return $"Added {itemReference} to Up Next. {skipReason}.";
        }

        return $"Added {itemReference} to Up Next.";
    }

    private static string FormatPartialFailureMessage(IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return $"Loaded candidates from included repositories, but {failures.Count} {noun} failed: {repositories}.";
    }

    private async Task<bool> ConfirmCapacityExceededAsync()
    {
        var result = await DialogService.ShowMessageBoxAsync(
            "Exceed capacity limit?",
            "Active load is already at or above your capacity limit. Add this item anyway?",
            yesText: "Add anyway",
            cancelText: "Cancel").ConfigureAwait(false);

        return result == true;
    }

    private static double GetCapacityProgressValue(IterationPlanningViewDto view)
    {
        if (view.Capacity <= 0)
        {
            return 0;
        }

        var percentage = (double)view.ActiveLoad / view.Capacity * 100;
        return Math.Min(percentage, 100);
    }

    private static string FormatCapacityProgressAriaLabel(IterationPlanningViewDto view) =>
        $"Capacity progress: {view.ActiveLoad} of {view.Capacity}";

    private static string FormatStallAge(int ageInDays) =>
        ageInDays == 1 ? "1 day" : $"{ageInDays} days";
}
