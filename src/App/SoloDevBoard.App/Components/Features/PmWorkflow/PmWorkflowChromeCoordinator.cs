using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>
/// Scoped coordinator that loads, caches, and cancels Planning chrome data for the current Blazor circuit.
/// </summary>
public sealed class PmWorkflowChromeCoordinator
{
    private readonly IPmSettingsService _pmSettingsService;
    private readonly IRepositoryService _repositoryService;
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService;
    private readonly ILogger<PmWorkflowChromeCoordinator> _logger;
    private CancellationTokenSource? _loadCancellation;
    private Task? _inFlightLoad;

    /// <summary>Initialises a new instance of the <see cref="PmWorkflowChromeCoordinator"/> class.</summary>
    /// <param name="pmSettingsService">The PM settings service.</param>
    /// <param name="repositoryService">The repository catalogue service.</param>
    /// <param name="projectBoardDiscoveryService">The planning board discovery service.</param>
    /// <param name="logger">The logger.</param>
    public PmWorkflowChromeCoordinator(
        IPmSettingsService pmSettingsService,
        IRepositoryService repositoryService,
        IPmProjectBoardDiscoveryService projectBoardDiscoveryService,
        ILogger<PmWorkflowChromeCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(pmSettingsService);
        ArgumentNullException.ThrowIfNull(repositoryService);
        ArgumentNullException.ThrowIfNull(projectBoardDiscoveryService);
        ArgumentNullException.ThrowIfNull(logger);

        _pmSettingsService = pmSettingsService;
        _repositoryService = repositoryService;
        _projectBoardDiscoveryService = projectBoardDiscoveryService;
        _logger = logger;
    }

    /// <summary>Gets the shared chrome state for the current circuit.</summary>
    public PmWorkflowChromeState State { get; } = new();

    /// <summary>Gets a value indicating whether chrome data has loaded successfully at least once.</summary>
    public bool HasLoadedOnce { get; private set; }

    /// <summary>Gets the cached Daily Focus board state for the current circuit, if any.</summary>
    public DailyFocusBoardStateCacheEntry? DailyFocusBoardState { get; private set; }

    /// <summary>Gets the cached Daily Focus stalled-review snapshot for the current circuit, if any.</summary>
    public DailyFocusStalledReviewsCacheEntry? DailyFocusStalledReviews { get; private set; }

    /// <summary>Gets the cached Daily Focus recommendations for the current circuit, if any.</summary>
    public DailyFocusRecommendationsCacheEntry? DailyFocusRecommendations { get; private set; }

    /// <summary>Gets the cached Backlog Review grouping for the current circuit, if any.</summary>
    public BacklogReviewCacheEntry? BacklogReview { get; private set; }

    /// <summary>Gets the cached Iteration Planning view for the current circuit, if any.</summary>
    public IterationPlanningCacheEntry? IterationPlanning { get; private set; }

    /// <summary>Loads chrome data when it has not been loaded yet for this circuit.</summary>
    /// <returns>A task that completes when the load attempt finishes or is skipped.</returns>
    public Task EnsureLoadedAsync() =>
        HasLoadedOnce && string.IsNullOrWhiteSpace(State.LoadErrorMessage)
            ? Task.CompletedTask
            : RefreshAsync(forceReload: false);

    /// <summary>Reloads chrome data, reusing in-flight work when already running.</summary>
    /// <param name="forceReload">When <see langword="true" />, invalidates Daily Focus board cache.</param>
    /// <returns>A task that completes when the load attempt finishes.</returns>
    public Task RefreshAsync(bool forceReload = true)
    {
        if (_inFlightLoad is { IsCompleted: false })
        {
            return _inFlightLoad;
        }

        if (forceReload)
        {
            ClearDailyFocusBoardState();
            ClearDailyFocusStalledReviews();
            ClearDailyFocusRecommendations();
            ClearBacklogReview();
            ClearIterationPlanning();
        }

        CancelPendingLoad();
        _loadCancellation = new CancellationTokenSource();
        var showBlockingLoader = !HasLoadedOnce;
        _inFlightLoad = LoadCoreAsync(_loadCancellation.Token, showBlockingLoader);
        return _inFlightLoad;
    }

    /// <summary>Persists updated PM settings and refreshes the in-memory chrome snapshot.</summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task that completes when settings are saved.</returns>
    public async Task SaveSettingsAsync(PmSettingsDto settings)
    {
        await _pmSettingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
        State.Settings = await _pmSettingsService.GetSettingsAsync().ConfigureAwait(false);
        ClearDailyFocusStalledReviews();
        ClearDailyFocusRecommendations();
        ClearBacklogReview();
        ClearIterationPlanning();
        State.MarkDataChanged();
    }

    /// <summary>Stores Daily Focus board state in the circuit cache.</summary>
    /// <param name="boardId">The planning board node identifier.</param>
    /// <param name="capacity">The active-load capacity denominator.</param>
    /// <param name="stallDays">The inclusive Up Next stall threshold in days.</param>
    /// <param name="state">The loaded board state, when successful.</param>
    /// <param name="errorMessage">The load error message, when unsuccessful.</param>
    /// <param name="isLoading">Whether a load is currently in flight.</param>
    public void SetDailyFocusBoardState(
        string boardId,
        int capacity,
        int stallDays,
        DailyFocusBoardStateDto? state,
        string? errorMessage,
        bool isLoading)
    {
        DailyFocusBoardState = new DailyFocusBoardStateCacheEntry(
            boardId,
            capacity,
            stallDays,
            state,
            errorMessage,
            isLoading);
    }

    /// <summary>Clears cached Daily Focus board state.</summary>
    public void ClearDailyFocusBoardState() => DailyFocusBoardState = null;

    /// <summary>Stores Daily Focus stalled reviews in the circuit cache.</summary>
    /// <param name="boardId">The planning board node identifier.</param>
    /// <param name="stallDays">The inclusive stall threshold in days.</param>
    /// <param name="excludedRepositories">Repositories omitted from stall alerts, in <c>owner/name</c> form.</param>
    /// <param name="snapshot">The loaded snapshot, when successful.</param>
    /// <param name="errorMessage">The load error message, when unsuccessful.</param>
    /// <param name="isLoading">Whether a load is currently in flight.</param>
    public void SetDailyFocusStalledReviews(
        string boardId,
        int stallDays,
        IReadOnlyList<string> excludedRepositories,
        DailyFocusStalledReviewSnapshotDto? snapshot,
        string? errorMessage,
        bool isLoading)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        ArgumentNullException.ThrowIfNull(excludedRepositories);

        DailyFocusStalledReviews = new DailyFocusStalledReviewsCacheEntry(
            boardId,
            stallDays,
            [.. excludedRepositories],
            snapshot,
            errorMessage,
            isLoading);
    }

    /// <summary>Clears cached Daily Focus stalled reviews.</summary>
    public void ClearDailyFocusStalledReviews() => DailyFocusStalledReviews = null;

    /// <summary>Stores Daily Focus recommendations in the circuit cache.</summary>
    /// <param name="boardId">The planning board node identifier.</param>
    /// <param name="recommendations">The ranked recommendations, when successful.</param>
    /// <param name="errorMessage">The load error message, when unsuccessful.</param>
    /// <param name="isLoading"><see langword="true"/> when a load is currently in flight; otherwise, <see langword="false"/>.</param>
    /// <param name="warningMessage">A partial-catalogue warning, when ranking proceeded without every repository.</param>
    public void SetDailyFocusRecommendations(
        string boardId,
        IReadOnlyList<DailyFocusRecommendationDto>? recommendations,
        string? errorMessage,
        bool isLoading,
        string? warningMessage = null)
    {
        DailyFocusRecommendations = new DailyFocusRecommendationsCacheEntry(
            boardId,
            recommendations,
            errorMessage,
            isLoading,
            warningMessage);
    }

    /// <summary>Clears cached Daily Focus recommendations.</summary>
    public void ClearDailyFocusRecommendations() => DailyFocusRecommendations = null;

    /// <summary>Stores Backlog Review grouping in the circuit cache.</summary>
    /// <param name="boardId">The planning board node identifier.</param>
    /// <param name="result">The grouped backlog, when successful.</param>
    /// <param name="errorMessage">The load error message, when unsuccessful.</param>
    /// <param name="isLoading"><see langword="true"/> when a load is currently in flight; otherwise, <see langword="false"/>.</param>
    /// <param name="warningMessage">A partial-catalogue warning, when grouping proceeded without every repository.</param>
    public void SetBacklogReview(
        string boardId,
        BacklogReviewResultDto? result,
        string? errorMessage,
        bool isLoading,
        string? warningMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);

        BacklogReview = new BacklogReviewCacheEntry(
            boardId,
            result,
            errorMessage,
            isLoading,
            warningMessage);
    }

    /// <summary>Clears cached Backlog Review grouping.</summary>
    public void ClearBacklogReview() => BacklogReview = null;

    /// <summary>Stores Iteration Planning view data in the circuit cache.</summary>
    /// <param name="boardId">The planning board node identifier.</param>
    /// <param name="view">The loaded planning view, when successful.</param>
    /// <param name="errorMessage">The load error message, when unsuccessful.</param>
    /// <param name="isLoading"><see langword="true"/> when a load is currently in flight; otherwise, <see langword="false"/>.</param>
    public void SetIterationPlanning(
        string boardId,
        IterationPlanningViewDto? view,
        string? errorMessage,
        bool isLoading)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);

        IterationPlanning = new IterationPlanningCacheEntry(boardId, view, errorMessage, isLoading);
    }

    /// <summary>Clears cached Iteration Planning view data.</summary>
    public void ClearIterationPlanning() => IterationPlanning = null;

    /// <summary>Cancels any in-flight chrome load, for example when leaving Planning.</summary>
    public void CancelPendingLoad()
    {
        if (_loadCancellation is null)
        {
            return;
        }

        _loadCancellation.Cancel();
        _loadCancellation.Dispose();
        _loadCancellation = null;
        State.IsLoading = false;
        State.IsRefreshing = false;
    }

    private async Task LoadCoreAsync(CancellationToken cancellationToken, bool showBlockingLoader)
    {
        if (showBlockingLoader)
        {
            State.IsLoading = true;
        }
        else
        {
            State.IsRefreshing = true;
        }

        State.LoadErrorMessage = null;

        try
        {
            State.Settings = await _pmSettingsService.GetSettingsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            State.ActiveRepositories = await _repositoryService.GetActiveRepositoriesAsync(cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var discovery = await _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
                    State.ActiveRepositories,
                    cancellationToken)
                .ConfigureAwait(false);

            State.PlanningBoardOptions = discovery.Options;
            State.InaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);

            var selectedPlanningBoardId = State.Settings.PlanningBoardNodeId ?? string.Empty;
            if (!State.PlanningBoardOptions.Any(option =>
                    option.Id.Equals(selectedPlanningBoardId, StringComparison.Ordinal)))
            {
                selectedPlanningBoardId = State.PlanningBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
                if (!string.Equals(State.Settings.PlanningBoardNodeId, selectedPlanningBoardId, StringComparison.Ordinal))
                {
                    await SaveSettingsAsync(State.Settings with
                    {
                        PlanningBoardNodeId = string.IsNullOrWhiteSpace(selectedPlanningBoardId)
                            ? null
                            : selectedPlanningBoardId,
                    }).ConfigureAwait(false);
                }
            }

            State.LastRefreshedAtUtc = DateTimeOffset.UtcNow;
            State.MarkDataChanged();
            HasLoadedOnce = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("PM Workflow chrome load cancelled.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load PM Workflow chrome data.");
            State.LoadErrorMessage =
                "Unable to load Planning settings. Check your GitHub connection and try again.";
        }
        finally
        {
            State.IsLoading = false;
            State.IsRefreshing = false;
        }
    }
}

/// <summary>Cached Daily Focus board state for a planning board within the current circuit.</summary>
/// <param name="BoardId">The planning board node identifier.</param>
/// <param name="Capacity">The active-load capacity denominator.</param>
/// <param name="StallDays">The inclusive Up Next stall threshold in days.</param>
/// <param name="State">The loaded board state, when successful.</param>
/// <param name="ErrorMessage">The load error message, when unsuccessful.</param>
/// <param name="IsLoading">Whether a load is currently in flight.</param>
public sealed record DailyFocusBoardStateCacheEntry(
    string BoardId,
    int Capacity,
    int StallDays,
    DailyFocusBoardStateDto? State,
    string? ErrorMessage,
    bool IsLoading);

/// <summary>Cached Daily Focus stalled-review snapshot for a planning board within the current circuit.</summary>
/// <param name="BoardId">The planning board node identifier.</param>
/// <param name="StallDays">The inclusive stall threshold in days.</param>
/// <param name="ExcludedRepositories">Repositories omitted from stall alerts, in <c>owner/name</c> form.</param>
/// <param name="Snapshot">The loaded snapshot, when successful.</param>
/// <param name="ErrorMessage">The load error message, when unsuccessful.</param>
/// <param name="IsLoading">Whether a load is currently in flight.</param>
public sealed record DailyFocusStalledReviewsCacheEntry(
    string BoardId,
    int StallDays,
    IReadOnlyList<string> ExcludedRepositories,
    DailyFocusStalledReviewSnapshotDto? Snapshot,
    string? ErrorMessage,
    bool IsLoading);

/// <summary>Cached Daily Focus recommendations for a planning board within the current circuit.</summary>
/// <param name="BoardId">The planning board node identifier.</param>
/// <param name="Recommendations">The ranked recommendations, when successful.</param>
/// <param name="ErrorMessage">The load error message, when unsuccessful.</param>
/// <param name="IsLoading">Whether a load is currently in flight.</param>
/// <param name="WarningMessage">A partial-catalogue warning, when ranking proceeded without every repository.</param>
public sealed record DailyFocusRecommendationsCacheEntry(
    string BoardId,
    IReadOnlyList<DailyFocusRecommendationDto>? Recommendations,
    string? ErrorMessage,
    bool IsLoading,
    string? WarningMessage = null);

/// <summary>Cached Backlog Review grouping for a planning board within the current circuit.</summary>
/// <param name="BoardId">The planning board node identifier.</param>
/// <param name="Result">The grouped backlog, when successful.</param>
/// <param name="ErrorMessage">The load error message, when unsuccessful.</param>
/// <param name="IsLoading">Whether a load is currently in flight.</param>
/// <param name="WarningMessage">A partial-catalogue warning, when grouping proceeded without every repository.</param>
public sealed record BacklogReviewCacheEntry(
    string BoardId,
    BacklogReviewResultDto? Result,
    string? ErrorMessage,
    bool IsLoading,
    string? WarningMessage = null);

/// <summary>Cached Iteration Planning view for a planning board within the current circuit.</summary>
/// <param name="BoardId">The planning board node identifier.</param>
/// <param name="View">The loaded planning view, when successful.</param>
/// <param name="ErrorMessage">The load error message, when unsuccessful.</param>
/// <param name="IsLoading">Whether a load is currently in flight.</param>
public sealed record IterationPlanningCacheEntry(
    string BoardId,
    IterationPlanningViewDto? View,
    string? ErrorMessage,
    bool IsLoading);
