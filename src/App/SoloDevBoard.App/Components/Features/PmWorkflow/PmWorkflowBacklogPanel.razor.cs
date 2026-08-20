using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Backlog Review grouping panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
public partial class PmWorkflowBacklogPanel : ComponentBase
{
    internal const string AllTypesFilter = "";
    internal const string IssueTypeFilter = "issue";
    internal const string PullRequestTypeFilter = "pr";
    internal const string AllRepositoriesFilter = "";

    [CascadingParameter]
    public PmWorkflowChromeState? ChromeState { get; set; }

    [CascadingParameter(Name = "PmWorkflowDataRevision")]
    public int DataRevision { get; set; }

    [Inject]
    public PmWorkflowChromeCoordinator ChromeCoordinator { get; set; } = default!;

    [Inject]
    public IBacklogReviewService BacklogReviewService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowBacklogPanel> Logger { get; set; } = default!;

    private BacklogReviewResultDto? snapshot;
    private bool isLoading;
    private string? loadErrorMessage;
    private string? warningMessage;
    private string selectedTypeFilter = AllTypesFilter;
    private string selectedRepositoryFilter = AllRepositoriesFilter;
    private string searchText = string.Empty;
    private int loadGeneration;

    private int NeglectDaysThreshold
        => ChromeState?.Settings.NeglectDays > 0
            ? ChromeState.Settings.NeglectDays
            : PmSettingsDefaults.NeglectDays;

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        if (ChromeState is null || ChromeState.IsLoading)
        {
            return Task.CompletedTask;
        }

        if (!ChromeState.HasPlanningBoardSelected)
        {
            snapshot = null;
            loadErrorMessage = null;
            warningMessage = null;
            return Task.CompletedTask;
        }

        var boardId = ChromeState.Settings.PlanningBoardNodeId!;
        if (TryApplyCachedResult(boardId))
        {
            return Task.CompletedTask;
        }

        _ = LoadAsync(boardId);
        return Task.CompletedTask;
    }

    private IReadOnlyList<string> RepositoryOptions
        => snapshot is null
            ? []
            : snapshot.Urgent
                .Concat(snapshot.ReadyToStart)
                .Concat(snapshot.AwaitingTriage)
                .Concat(snapshot.BlockedOrDeferred)
                .Select(static item => item.RepositoryFullName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private IReadOnlyList<BacklogReviewItemDto> FilteredUrgent => Filter(snapshot?.Urgent);

    private IReadOnlyList<BacklogReviewItemDto> FilteredReady => Filter(snapshot?.ReadyToStart);

    private IReadOnlyList<BacklogReviewItemDto> FilteredAwaitingTriage => Filter(snapshot?.AwaitingTriage);

    private IReadOnlyList<BacklogReviewItemDto> FilteredBlocked => Filter(snapshot?.BlockedOrDeferred);

    private IReadOnlyList<BacklogEpicNearCompleteItemDto> FilteredEpicsNearComplete
        => FilterEpics(snapshot?.EpicsNearComplete);

    private IReadOnlyList<BacklogNeglectedRepositoryDto> FilteredNeglectedRepositories
        => FilterNeglected(snapshot?.NeglectedRepositories);

    private bool IsCatalogueEmpty
        => snapshot is not null
            && snapshot.Urgent.Count == 0
            && snapshot.ReadyToStart.Count == 0
            && snapshot.AwaitingTriage.Count == 0
            && snapshot.BlockedOrDeferred.Count == 0
            && snapshot.EpicsNearComplete.Count == 0
            && snapshot.NeglectedRepositories.Count == 0;

    private bool HasNoFilteredItems
        => !IsCatalogueEmpty
            && FilteredUrgent.Count == 0
            && FilteredReady.Count == 0
            && FilteredAwaitingTriage.Count == 0
            && FilteredBlocked.Count == 0
            && FilteredEpicsNearComplete.Count == 0
            && FilteredNeglectedRepositories.Count == 0;

    private bool TryApplyCachedResult(string boardId)
    {
        var cached = ChromeCoordinator.BacklogReview;
        if (cached is null || !cached.BoardId.Equals(boardId, StringComparison.Ordinal))
        {
            return false;
        }

        isLoading = cached.IsLoading;
        snapshot = cached.Result;
        loadErrorMessage = cached.ErrorMessage;
        warningMessage = cached.WarningMessage;
        return cached.IsLoading
            || cached.Result is not null
            || !string.IsNullOrWhiteSpace(cached.ErrorMessage);
    }

    private Task RetryLoadAsync()
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(ChromeState.Settings.PlanningBoardNodeId))
        {
            return Task.CompletedTask;
        }

        ChromeCoordinator.ClearBacklogReview();
        return LoadAsync(ChromeState.Settings.PlanningBoardNodeId);
    }

    private async Task LoadAsync(string boardId)
    {
        if (TryApplyCachedResult(boardId))
        {
            return;
        }

        var generation = Interlocked.Increment(ref loadGeneration);
        isLoading = true;
        loadErrorMessage = null;
        warningMessage = null;
        snapshot = null;
        ChromeCoordinator.SetBacklogReview(boardId, null, null, isLoading: true);

        try
        {
            var result = await BacklogReviewService.GetBacklogAsync(boardId).ConfigureAwait(false);
            if (!ShouldApplyResult(generation, boardId))
            {
                return;
            }

            snapshot = result;
            warningMessage = FormatPartialFailureWarning(result.Failures);
            ChromeCoordinator.SetBacklogReview(boardId, result, null, isLoading: false, warningMessage);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load Backlog Review groups.");
            if (!ShouldApplyResult(generation, boardId))
            {
                return;
            }

            snapshot = null;
            warningMessage = null;
            loadErrorMessage = "Unable to load the backlog. Check your GitHub connection and try again.";
            ChromeCoordinator.SetBacklogReview(boardId, null, loadErrorMessage, isLoading: false);
        }
        finally
        {
            if (generation == loadGeneration)
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }
    }

    private bool ShouldApplyResult(int generation, string boardId)
    {
        if (generation != loadGeneration)
        {
            return false;
        }

        var currentBoardId = ChromeState?.Settings.PlanningBoardNodeId;
        return !string.IsNullOrWhiteSpace(currentBoardId)
            && currentBoardId.Equals(boardId, StringComparison.Ordinal);
    }

    internal Task OnTypeFilterChanged(string value)
    {
        selectedTypeFilter = value ?? AllTypesFilter;
        StateHasChanged();
        return Task.CompletedTask;
    }

    internal Task OnRepositoryFilterChanged(string value)
    {
        selectedRepositoryFilter = value ?? AllRepositoriesFilter;
        StateHasChanged();
        return Task.CompletedTask;
    }

    internal Task OnSearchChanged(string value)
    {
        searchText = value ?? string.Empty;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private IReadOnlyList<BacklogReviewItemDto> Filter(IReadOnlyList<BacklogReviewItemDto>? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        return items.Where(MatchesFilters).ToArray();
    }

    private IReadOnlyList<BacklogEpicNearCompleteItemDto> FilterEpics(
        IReadOnlyList<BacklogEpicNearCompleteItemDto>? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        return items.Where(MatchesEpicFilters).ToArray();
    }

    private IReadOnlyList<BacklogNeglectedRepositoryDto> FilterNeglected(
        IReadOnlyList<BacklogNeglectedRepositoryDto>? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(selectedRepositoryFilter)
            && !items.Any(repository =>
                repository.FullName.Equals(selectedRepositoryFilter, StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return items.ToArray();
        }

        var query = searchText.Trim();
        return items
            .Where(repository => repository.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private bool MatchesFilters(BacklogReviewItemDto item)
    {
        if (selectedTypeFilter == IssueTypeFilter && item.ItemType != PmWorkItemTypeDto.Issue)
        {
            return false;
        }

        if (selectedTypeFilter == PullRequestTypeFilter && item.ItemType != PmWorkItemTypeDto.PullRequest)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selectedRepositoryFilter)
            && !item.RepositoryFullName.Equals(selectedRepositoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var query = searchText.Trim();
        return item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.RepositoryFullName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Number.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || $"{item.RepositoryFullName}#{item.Number}".Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesEpicFilters(BacklogEpicNearCompleteItemDto epic)
    {
        if (!string.IsNullOrWhiteSpace(selectedRepositoryFilter)
            && !epic.RepositoryFullName.Equals(selectedRepositoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var query = searchText.Trim();
        return epic.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || epic.RepositoryFullName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || epic.Number.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || $"{epic.RepositoryFullName}#{epic.Number}".Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatItemKindChip(PmWorkItemTypeDto itemType)
        => itemType == PmWorkItemTypeDto.PullRequest ? "Pull request" : "Issue";

    private static Color FormatItemKindChipColor(PmWorkItemTypeDto itemType)
        => itemType == PmWorkItemTypeDto.PullRequest ? Color.Warning : Color.Info;

    private static Variant FormatItemKindChipVariant(PmWorkItemTypeDto itemType)
        => itemType == PmWorkItemTypeDto.PullRequest ? Variant.Outlined : Variant.Filled;

    private static string FormatPriority(string? priorityLabel)
        => string.IsNullOrWhiteSpace(priorityLabel) ? "Unlabelled" : priorityLabel;

    private static string FormatLastActivity(DateTimeOffset? lastActivityAt)
    {
        if (lastActivityAt is null || lastActivityAt == default)
        {
            return "Never";
        }

        return lastActivityAt.Value.ToString("d MMM yyyy");
    }

    private static string? FormatPartialFailureWarning(IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        if (failures.Count == 0)
        {
            return null;
        }

        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return $"The backlog was grouped without {failures.Count} {noun} that failed to load: {repositories}.";
    }
}
