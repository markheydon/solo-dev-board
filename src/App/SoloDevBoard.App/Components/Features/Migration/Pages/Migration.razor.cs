using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Feedback;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.Migration.Pages;

/// <summary>Provides the One-Click Migration workflow for labels, milestones, and project board columns.</summary>
public partial class Migration : ComponentBase
{
    private const string CreateNewBoardOptionValue = "__create_new__";

    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the migration service used to preview and apply migration.</summary>
    [Inject]
    public IMigrationService MigrationService { get; set; } = default!;

    /// <summary>Gets or sets the logger for migration diagnostics.</summary>
    [Inject]
    public ILogger<Migration> Logger { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service for user notifications.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private IReadOnlyList<RepositoryDto> selectedRepositories = [];
    private string sourceRepositoryFullName = string.Empty;
    private HashSet<string> targetRepositoryFullNames = new(StringComparer.OrdinalIgnoreCase);
    private bool migrateLabels = true;
    private bool migrateMilestones = true;
    private bool migrateProjectBoardColumns;
    private bool keepAreaLabels = true;
    private bool ignoreAreaLabels = true;
    private string sourceProjectBoardId = string.Empty;
    private Dictionary<string, string> targetProjectBoardSelections = new(StringComparer.OrdinalIgnoreCase);
    private MigrationProjectBoardDiscoveryDto? sourceBoardDiscovery;
    private Dictionary<string, MigrationProjectBoardDiscoveryDto> targetBoardDiscoveries = new(StringComparer.OrdinalIgnoreCase);
    private bool isLoadingSourceBoards;
    private bool isLoadingTargetBoards;
    private CancellationTokenSource? _sourceProjectBoardsLoadCts;
    private CancellationTokenSource? _targetProjectBoardsLoadCts;
    private MigrationConflictStrategy conflictStrategy = MigrationConflictStrategy.Skip;
    private MigrationPreviewDto previewResult = new(MigrationConflictStrategy.Skip, [], [], []);
    private MigrationResultDto applyResult = new(MigrationConflictStrategy.Skip, [], [], []);
    private bool isLoadingRepositories = true;
    private bool isPreviewing;
    private bool isApplying;
    private bool showPreview;
    private string? inaccessibleProjectBoardsWarning;
    private bool hasRepositoryLoadFailure;
    private string? repositoryLoadErrorMessage;
    private bool isReloadingFromGitHub;

    private void ShowSnackbarFeedback(string message, Severity severity)
        => SnackbarFeedback.Show(Snackbar, message, severity);

    private static readonly IReadOnlyList<ConflictOption> conflictOptions =
    [
        new(MigrationConflictStrategy.Skip, "Skip", "Create missing items and keep existing conflicts unchanged."),
        new(MigrationConflictStrategy.Overwrite, "Overwrite", "Replace conflicting items and remove target-only items."),
        new(MigrationConflictStrategy.Merge, "Merge", "Create missing items and update conflicting fields while preserving target-only items."),
    ];

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task ReloadRepositoriesAsync()
        => await RetryLoadRepositoriesAsync();

    private async Task ReloadFromGitHubAsync()
    {
        if (isLoadingRepositories || isReloadingFromGitHub || isPreviewing || isApplying)
        {
            return;
        }

        var preservedRepositoryFullNames = selectedRepositoryFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preservedSource = sourceRepositoryFullName;
        var preservedTargets = targetRepositoryFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preservedSourceBoardId = sourceProjectBoardId;
        var preservedTargetBoardSelections = new Dictionary<string, string>(targetProjectBoardSelections, StringComparer.OrdinalIgnoreCase);
        var preservedMigrateLabels = migrateLabels;
        var preservedMigrateMilestones = migrateMilestones;
        var preservedMigrateProjectBoardColumns = migrateProjectBoardColumns;
        var preservedKeepAreaLabels = keepAreaLabels;
        var preservedIgnoreAreaLabels = ignoreAreaLabels;
        var preservedConflictStrategy = conflictStrategy;

        isReloadingFromGitHub = true;

        try
        {
            await RefreshRepositoriesCatalogueAsync(forceReload: true);

            if (preservedRepositoryFullNames.Count > 0)
            {
                selectedRepositories = availableRepositories
                    .Where(repository => preservedRepositoryFullNames.Contains(repository.FullName))
                    .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                sourceRepositoryFullName = selectedRepositories.Any(repository =>
                    repository.FullName.Equals(preservedSource, StringComparison.OrdinalIgnoreCase))
                    ? preservedSource
                    : string.Empty;

                targetRepositoryFullNames = preservedTargets
                    .Where(target => selectedRepositories.Any(repository => repository.FullName.Equals(target, StringComparison.OrdinalIgnoreCase))
                        && !target.Equals(sourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                migrateLabels = preservedMigrateLabels;
                migrateMilestones = preservedMigrateMilestones;
                migrateProjectBoardColumns = preservedMigrateProjectBoardColumns;
                keepAreaLabels = preservedKeepAreaLabels;
                ignoreAreaLabels = preservedIgnoreAreaLabels;
                conflictStrategy = preservedConflictStrategy;

                EnsureSelectionState();
                await LoadBoardOptionsAsync();

                if (!string.IsNullOrWhiteSpace(sourceProjectBoardId)
                    && sourceBoardDiscovery?.Options.Any(option => option.Id.Equals(preservedSourceBoardId, StringComparison.Ordinal)) == true)
                {
                    sourceProjectBoardId = preservedSourceBoardId;
                }

                foreach (var (targetRepository, boardId) in preservedTargetBoardSelections)
                {
                    if (targetBoardDiscoveries.TryGetValue(targetRepository, out var discovery)
                        && (boardId.Equals(CreateNewBoardOptionValue, StringComparison.Ordinal)
                            || discovery.Options.Any(option => option.Id.Equals(boardId, StringComparison.Ordinal))))
                    {
                        targetProjectBoardSelections[targetRepository] = boardId;
                    }
                }
            }
        }
        finally
        {
            isReloadingFromGitHub = false;
        }
    }

    private async Task RetryLoadRepositoriesAsync()
    {
        await RefreshRepositoriesCatalogueAsync(forceReload: true);
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        repositoryLoadErrorMessage = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedRepositories = [];
            ResetWorkflow();
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
            Logger.LogError(ex, "GitHub API request failed while loading migration repositories.");
            availableRepositories = [];
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load migration repositories.");
            availableRepositories = [];
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private async Task RefreshRepositoriesCatalogueAsync(bool forceReload)
    {
        hasRepositoryLoadFailure = false;
        repositoryLoadErrorMessage = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync(forceReload: forceReload))
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
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
            Logger.LogError(ex, "GitHub API request failed while refreshing migration repositories.");
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh migration repositories.");
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories.";
        }
    }

    private bool ShowReloadFromGitHubButton => !isLoadingRepositories;

    private bool IsReloadFromGitHubDisabled => isReloadingFromGitHub || isPreviewing || isApplying;

    private async Task OnSelectedRepositoriesChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var selectedNames = repositoryFullNames
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        selectedRepositories = availableRepositories
            .Where(repository => selectedNames.Contains(repository.FullName))
            .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        EnsureSelectionState();
        await LoadBoardOptionsAsync();
    }

    private async Task OnSourceRepositoryChangedAsync(string value)
    {
        sourceRepositoryFullName = value ?? string.Empty;
        _ = targetRepositoryFullNames.Remove(sourceRepositoryFullName);
        ResetPreviewAndResults();
        await LoadBoardOptionsAsync();
    }

    private async Task OnTargetRepositoryChangedAsync(string repositoryFullName, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(repositoryFullName) || repositoryFullName.Equals(sourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (isSelected)
        {
            _ = targetRepositoryFullNames.Add(repositoryFullName);
        }
        else
        {
            _ = targetRepositoryFullNames.Remove(repositoryFullName);
            _ = targetProjectBoardSelections.Remove(repositoryFullName);
            _ = targetBoardDiscoveries.Remove(repositoryFullName);
        }

        ResetPreviewAndResults();
        await LoadBoardOptionsAsync();
    }

    private Task OnConflictStrategyChangedAsync(MigrationConflictStrategy value)
    {
        conflictStrategy = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnKeepAreaLabelsChangedAsync(bool value)
    {
        keepAreaLabels = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnIgnoreAreaLabelsChangedAsync(bool value)
    {
        ignoreAreaLabels = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private async Task OnMigrateLabelsChangedAsync(bool value)
    {
        migrateLabels = value;
        ResetPreviewAndResults();
        await Task.CompletedTask;
    }

    private async Task OnMigrateMilestonesChangedAsync(bool value)
    {
        migrateMilestones = value;
        ResetPreviewAndResults();
        await Task.CompletedTask;
    }

    private async Task OnMigrateProjectBoardColumnsChangedAsync(bool value)
    {
        migrateProjectBoardColumns = value;
        ResetPreviewAndResults();

        if (migrateProjectBoardColumns)
        {
            await LoadBoardOptionsAsync();
        }
        else
        {
            ClearBoardSelectionState();
        }
    }

    private async Task OnSourceProjectBoardChangedAsync(string value)
    {
        sourceProjectBoardId = value ?? string.Empty;
        ResetPreviewAndResults();
        await Task.CompletedTask;
    }

    private async Task OnTargetProjectBoardChangedAsync(string repositoryFullName, string value)
    {
        if (string.IsNullOrWhiteSpace(repositoryFullName))
        {
            return;
        }

        targetProjectBoardSelections[repositoryFullName] = value ?? string.Empty;
        ResetPreviewAndResults();
        await Task.CompletedTask;
    }

    private async Task LoadBoardOptionsAsync()
    {
        if (!migrateProjectBoardColumns)
        {
            return;
        }

        await LoadSourceBoardOptionsAsync();

        var targetNames = targetRepositoryFullNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var targetLoadCts = BeginTargetProjectBoardsLoad();
        var targetCancellationToken = targetLoadCts.Token;
        isLoadingTargetBoards = targetNames.Length > 0;

        try
        {
            foreach (var targetRepositoryFullName in targetNames)
            {
                await LoadTargetBoardOptionsAsync(targetRepositoryFullName, targetLoadCts, targetCancellationToken);
            }
        }
        finally
        {
            if (ReferenceEquals(_targetProjectBoardsLoadCts, targetLoadCts))
            {
                isLoadingTargetBoards = false;
            }
        }

        if (!IsStaleTargetProjectBoardsBatch(targetLoadCts))
        {
            UpdateInaccessibleProjectBoardsWarning();
        }
    }

    private async Task LoadSourceBoardOptionsAsync()
    {
        if (!migrateProjectBoardColumns || string.IsNullOrWhiteSpace(sourceRepositoryFullName))
        {
            CancelBoardLoads();
            sourceBoardDiscovery = null;
            sourceProjectBoardId = string.Empty;
            isLoadingSourceBoards = false;
            return;
        }

        var expectedSourceRepositoryFullName = sourceRepositoryFullName;
        var coordinates = SplitRepositoryFullName(expectedSourceRepositoryFullName);
        var loadCts = BeginSourceProjectBoardsLoad();
        var cancellationToken = loadCts.Token;

        isLoadingSourceBoards = true;
        sourceBoardDiscovery = null;
        inaccessibleProjectBoardsWarning = null;

        try
        {
            var discovery = await MigrationService
                .GetProjectBoardOptionsAsync(coordinates.Owner, coordinates.Name, cancellationToken)
                .ConfigureAwait(false);

            if (IsStaleSourceProjectBoardsLoad(loadCts, expectedSourceRepositoryFullName))
            {
                return;
            }

            sourceBoardDiscovery = discovery;

            if (discovery.Options.Count == 1)
            {
                sourceProjectBoardId = discovery.Options[0].Id;
            }
            else if (!discovery.Options.Any(option => option.Id.Equals(sourceProjectBoardId, StringComparison.Ordinal)))
            {
                sourceProjectBoardId = string.Empty;
            }
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
            if (IsStaleSourceProjectBoardsLoad(loadCts, expectedSourceRepositoryFullName))
            {
                return;
            }

            Logger.LogError(ex, "GitHub API request failed while loading source project boards for {SourceRepository}.", expectedSourceRepositoryFullName);
            ShowSnackbarFeedback($"GitHub API request failed while loading source project boards. {ex.Message}", Severity.Error);
            sourceBoardDiscovery = null;
            sourceProjectBoardId = string.Empty;
        }
        catch (Exception ex)
        {
            if (IsStaleSourceProjectBoardsLoad(loadCts, expectedSourceRepositoryFullName))
            {
                return;
            }

            Logger.LogError(ex, "Failed to load source project boards for {SourceRepository}.", expectedSourceRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while loading source project boards.", Severity.Error);
            sourceBoardDiscovery = null;
            sourceProjectBoardId = string.Empty;
        }
        finally
        {
            if (ReferenceEquals(_sourceProjectBoardsLoadCts, loadCts))
            {
                isLoadingSourceBoards = false;
            }
        }
    }

    private async Task LoadTargetBoardOptionsAsync(
        string targetRepositoryFullName,
        CancellationTokenSource loadCts,
        CancellationToken cancellationToken)
    {
        if (IsStaleTargetProjectBoardsLoad(loadCts, targetRepositoryFullName))
        {
            return;
        }

        var coordinates = SplitRepositoryFullName(targetRepositoryFullName);

        try
        {
            var discovery = await MigrationService
                .GetProjectBoardOptionsAsync(coordinates.Owner, coordinates.Name, cancellationToken)
                .ConfigureAwait(false);

            if (IsStaleTargetProjectBoardsLoad(loadCts, targetRepositoryFullName))
            {
                return;
            }

            targetBoardDiscoveries[targetRepositoryFullName] = discovery;

            if (discovery.Options.Count == 1)
            {
                targetProjectBoardSelections[targetRepositoryFullName] = discovery.Options[0].Id;
            }
            else if (discovery.Options.Count == 0)
            {
                targetProjectBoardSelections[targetRepositoryFullName] = CreateNewBoardOptionValue;
            }
            else if (!targetProjectBoardSelections.TryGetValue(targetRepositoryFullName, out var selectedId)
                || (!selectedId.Equals(CreateNewBoardOptionValue, StringComparison.Ordinal)
                    && !discovery.Options.Any(option => option.Id.Equals(selectedId, StringComparison.Ordinal))))
            {
                targetProjectBoardSelections[targetRepositoryFullName] = string.Empty;
            }
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
            if (IsStaleTargetProjectBoardsLoad(loadCts, targetRepositoryFullName))
            {
                return;
            }

            Logger.LogError(ex, "GitHub API request failed while loading target project boards for {TargetRepository}.", targetRepositoryFullName);
            ShowSnackbarFeedback($"GitHub API request failed while loading target project boards. {ex.Message}", Severity.Error);
            _ = targetBoardDiscoveries.Remove(targetRepositoryFullName);
            _ = targetProjectBoardSelections.Remove(targetRepositoryFullName);
        }
        catch (Exception ex)
        {
            if (IsStaleTargetProjectBoardsLoad(loadCts, targetRepositoryFullName))
            {
                return;
            }

            Logger.LogError(ex, "Failed to load target project boards for {TargetRepository}.", targetRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while loading target project boards.", Severity.Error);
            _ = targetBoardDiscoveries.Remove(targetRepositoryFullName);
            _ = targetProjectBoardSelections.Remove(targetRepositoryFullName);
        }
    }

    private void UpdateInaccessibleProjectBoardsWarning()
    {
        var discoveries = new List<MigrationProjectBoardDiscoveryDto>();
        if (sourceBoardDiscovery is not null)
        {
            discoveries.Add(sourceBoardDiscovery);
        }

        discoveries.AddRange(targetBoardDiscoveries.Values);

        var inaccessibleDiscovery = discoveries
            .Where(discovery => discovery.InaccessibleLinkedProjectCount > 0)
            .OrderByDescending(discovery => discovery.InaccessibleLinkedProjectCount)
            .FirstOrDefault();

        inaccessibleProjectBoardsWarning = inaccessibleDiscovery is null
            ? null
            : LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                inaccessibleDiscovery.TotalLinkedProjectCount,
                inaccessibleDiscovery.InaccessibleLinkedProjectCount);
    }

    private async Task PreviewMigrationAsync()
    {
        if (isPreviewing)
        {
            return;
        }

        if (!CanPreview)
        {
            ShowSnackbarFeedback(GetPreviewBlockedMessage(), Severity.Warning);
            return;
        }

        isPreviewing = true;

        try
        {
            previewResult = await MigrationService.PreviewMigrationAsync(
                sourceRepositoryFullName,
                targetRepositoryFullNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
                BuildScope(),
                conflictStrategy,
                BuildBoardSelection(),
                keepAreaLabels,
                ignoreAreaLabels);

            showPreview = true;
            applyResult = new MigrationResultDto(conflictStrategy, [], [], []);
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
            Logger.LogError(ex, "GitHub API request failed while previewing migration from {SourceRepository}.", sourceRepositoryFullName);
            ShowSnackbarFeedback($"GitHub API request failed while previewing migration. {ex.Message}", Severity.Error);
            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], [], []);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview migration from {SourceRepository}.", sourceRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while previewing migration.", Severity.Error);
            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], [], []);
        }
        finally
        {
            isPreviewing = false;
        }
    }

    private void CancelPreview()
    {
        showPreview = false;
        previewResult = new MigrationPreviewDto(conflictStrategy, [], [], []);
        ShowSnackbarFeedback("Migration preview was cancelled. No changes were applied.", Severity.Info);
    }

    private async Task ApplyMigrationAsync()
    {
        if (!CanApply)
        {
            ShowSnackbarFeedback("Preview migration before applying changes.", Severity.Warning);
            return;
        }

        isApplying = true;

        try
        {
            applyResult = await MigrationService.ApplyMigrationAsync(
                sourceRepositoryFullName,
                targetRepositoryFullNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
                BuildScope(),
                conflictStrategy,
                BuildBoardSelection(),
                keepAreaLabels,
                ignoreAreaLabels);

            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], [], []);

            var labelFailures = applyResult.LabelResults.Count(result => result.HasError);
            var milestoneFailures = applyResult.MilestoneResults.Count(result => result.HasError);
            var statusFailures = applyResult.ProjectBoardStatusResults.Count(result => result.HasError);
            var totalFailures = labelFailures + milestoneFailures + statusFailures;

            if (totalFailures == 0)
            {
                ShowSnackbarFeedback("Migration completed successfully.", Severity.Success);
            }
            else
            {
                ShowSnackbarFeedback($"Migration completed with {totalFailures} repository operation errors.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply migration from {SourceRepository}.", sourceRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while applying migration.", Severity.Error);
        }
        finally
        {
            isApplying = false;
        }
    }

    private bool IsBusy => isLoadingRepositories || isPreviewing || isApplying || isLoadingSourceBoards || isLoadingTargetBoards;

    private bool HasValidScopeSelection => migrateLabels || migrateMilestones || migrateProjectBoardColumns;

    private bool HasCompleteBoardSelections
    {
        get
        {
            if (!migrateProjectBoardColumns)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(sourceProjectBoardId))
            {
                return false;
            }

            foreach (var targetRepositoryFullName in targetRepositoryFullNames)
            {
                if (!targetProjectBoardSelections.TryGetValue(targetRepositoryFullName, out var selection)
                    || string.IsNullOrWhiteSpace(selection))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private bool CanPreview => !IsBusy
        && selectedRepositories.Count >= 2
        && !string.IsNullOrWhiteSpace(sourceRepositoryFullName)
        && targetRepositoryFullNames.Count > 0
        && HasValidScopeSelection
        && HasCompleteBoardSelections;

    private bool CanApply => showPreview
        && !IsBusy
        && HasActionablePreviewChanges;

    private bool HasActionablePreviewChanges
    {
        get
        {
            var labelActions = previewResult.LabelPreviews.Sum(preview => preview.ToCreate.Count + preview.ToUpdate.Count + preview.ToDelete.Count);
            var milestoneActions = previewResult.MilestonePreviews.Sum(preview => preview.ToCreate.Count + preview.ToUpdate.Count + preview.ToDelete.Count);
            var statusActions = previewResult.ProjectBoardStatusPreviews.Sum(preview => preview.ToCreate.Count + preview.ToUpdate.Count + preview.ToDelete.Count);
            return labelActions + milestoneActions + statusActions > 0;
        }
    }

    private bool HasInaccessibleProjectBoardsWarning => !string.IsNullOrWhiteSpace(inaccessibleProjectBoardsWarning);

    private string RepositorySelectorSummary
    {
        get
        {
            var repositoryCount = availableRepositories.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {selectedRepositories.Count} selected. Archived repositories are hidden by default.";
        }
    }

    private string BoardSelectionRequirementsMessage
        => migrateProjectBoardColumns && !HasCompleteBoardSelections
            ? "Select a source project board and choose a target board (or create a new board) for each target repository to unlock preview."
            : "Select repositories and migration scope to unlock preview.";

    private IReadOnlyList<string> availableRepositoryFullNames
        => availableRepositories
            .Select(repository => repository.FullName)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> selectedRepositoryFullNames
        => selectedRepositories
            .Select(repository => repository.FullName)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> orderedTargetRepositoryFullNames
        => targetRepositoryFullNames
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> orderedPreviewRepositories
        => previewResult.LabelPreviews
            .Select(preview => preview.RepositoryFullName)
            .Union(previewResult.MilestonePreviews.Select(preview => preview.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .Union(previewResult.ProjectBoardStatusPreviews.Select(preview => preview.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> orderedApplyRepositories
        => applyResult.LabelResults
            .Select(result => result.RepositoryFullName)
            .Union(applyResult.MilestoneResults.Select(result => result.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .Union(applyResult.ProjectBoardStatusResults.Select(result => result.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private LabelSyncRepositoryPreviewDto GetLabelPreview(string repositoryFullName)
        => previewResult.LabelPreviews.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new LabelSyncRepositoryPreviewDto(repositoryFullName, [], [], [], [], [], []);

    private MilestoneSyncRepositoryPreviewDto GetMilestonePreview(string repositoryFullName)
        => previewResult.MilestonePreviews.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new MilestoneSyncRepositoryPreviewDto(repositoryFullName, [], [], [], []);

    private ProjectBoardStatusSyncRepositoryPreviewDto GetStatusPreview(string repositoryFullName)
        => previewResult.ProjectBoardStatusPreviews.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new ProjectBoardStatusSyncRepositoryPreviewDto(repositoryFullName, null, false, [], [], [], [], [], 0, 0);

    private LabelSyncRepositoryResultDto GetLabelResult(string repositoryFullName)
        => applyResult.LabelResults.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new LabelSyncRepositoryResultDto(repositoryFullName, 0, 0, 0, 0, null);

    private MilestoneSyncRepositoryResultDto GetMilestoneResult(string repositoryFullName)
        => applyResult.MilestoneResults.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new MilestoneSyncRepositoryResultDto(repositoryFullName, 0, 0, 0, 0, null);

    private ProjectBoardStatusSyncRepositoryResultDto GetStatusResult(string repositoryFullName)
        => applyResult.ProjectBoardStatusResults.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new ProjectBoardStatusSyncRepositoryResultDto(repositoryFullName, 0, 0, 0, 0, null, [], null);

    private IReadOnlyList<MigrationProjectBoardOptionDto> GetSourceBoardOptions()
        => sourceBoardDiscovery?.Options ?? [];

    private IReadOnlyList<MigrationProjectBoardOptionDto> GetTargetBoardOptions(string repositoryFullName)
        => targetBoardDiscoveries.TryGetValue(repositoryFullName, out var discovery)
            ? discovery.Options
            : [];

    private string GetTargetBoardSelection(string repositoryFullName)
        => targetProjectBoardSelections.TryGetValue(repositoryFullName, out var selection)
            ? selection
            : string.Empty;

    private static bool HasActionableChanges(
        LabelSyncRepositoryPreviewDto labelPreview,
        MilestoneSyncRepositoryPreviewDto milestonePreview,
        ProjectBoardStatusSyncRepositoryPreviewDto statusPreview)
        => (labelPreview.ToCreate.Count + labelPreview.ToUpdate.Count + labelPreview.ToDelete.Count
            + milestonePreview.ToCreate.Count + milestonePreview.ToUpdate.Count + milestonePreview.ToDelete.Count
            + statusPreview.ToCreate.Count + statusPreview.ToUpdate.Count + statusPreview.ToDelete.Count) > 0;

    private void ResetWorkflow()
    {
        sourceRepositoryFullName = string.Empty;
        targetRepositoryFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        migrateLabels = true;
        migrateMilestones = true;
        migrateProjectBoardColumns = false;
        keepAreaLabels = true;
        ignoreAreaLabels = true;
        conflictStrategy = MigrationConflictStrategy.Skip;
        ClearBoardSelectionState();
        ResetPreviewAndResults();
    }

    private void ClearBoardSelectionState()
    {
        CancelBoardLoads();
        sourceProjectBoardId = string.Empty;
        sourceBoardDiscovery = null;
        targetProjectBoardSelections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        targetBoardDiscoveries = new Dictionary<string, MigrationProjectBoardDiscoveryDto>(StringComparer.OrdinalIgnoreCase);
        inaccessibleProjectBoardsWarning = null;
        isLoadingSourceBoards = false;
        isLoadingTargetBoards = false;
    }

    private void ResetPreviewAndResults()
    {
        showPreview = false;
        previewResult = new MigrationPreviewDto(conflictStrategy, [], [], []);
        applyResult = new MigrationResultDto(conflictStrategy, [], [], []);
    }

    private void EnsureSelectionState()
    {
        var selectedRepositoryNames = selectedRepositories
            .Select(repository => repository.FullName)
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedRepositoryNames.Count == 0)
        {
            ResetWorkflow();
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceRepositoryFullName) || !selectedRepositoryNames.Contains(sourceRepositoryFullName))
        {
            sourceRepositoryFullName = selectedRepositoryNames
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        targetRepositoryFullNames = targetRepositoryFullNames
            .Where(selectedRepositoryNames.Contains)
            .Where(target => !target.Equals(sourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        targetProjectBoardSelections = targetProjectBoardSelections
            .Where(entry => targetRepositoryFullNames.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        targetBoardDiscoveries = targetBoardDiscoveries
            .Where(entry => targetRepositoryFullNames.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        ResetPreviewAndResults();
    }

    private MigrationScopeDto BuildScope() => new(migrateLabels, migrateMilestones, migrateProjectBoardColumns);

    private MigrationBoardSelectionDto? BuildBoardSelection()
    {
        if (!migrateProjectBoardColumns)
        {
            return null;
        }

        var targetSelections = targetRepositoryFullNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(targetRepositoryFullName =>
            {
                var selection = targetProjectBoardSelections[targetRepositoryFullName];
                if (selection.Equals(CreateNewBoardOptionValue, StringComparison.Ordinal))
                {
                    var repoName = SplitRepositoryFullName(targetRepositoryFullName).Name;
                    return new MigrationTargetBoardSelectionDto(targetRepositoryFullName, null, $"{repoName} board");
                }

                return new MigrationTargetBoardSelectionDto(targetRepositoryFullName, selection, null);
            })
            .ToArray();

        return new MigrationBoardSelectionDto(sourceProjectBoardId, targetSelections);
    }

    private string GetPreviewBlockedMessage()
    {
        if (!HasValidScopeSelection)
        {
            return "Select at least one migration item before previewing migration.";
        }

        if (migrateProjectBoardColumns && !HasCompleteBoardSelections)
        {
            return "Select a source project board and choose a target board (or create a new board) for each target repository before previewing migration.";
        }

        return "Select one source repository, at least one target repository, and at least one migration item before previewing migration.";
    }

    private static RepositoryCoordinates SplitRepositoryFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name must be provided.", nameof(fullName));
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Repository '{fullName}' must be in owner/repository format.", nameof(fullName));
        }

        return new RepositoryCoordinates(parts[0], parts[1]);
    }

    private CancellationTokenSource BeginSourceProjectBoardsLoad()
    {
        CancelTargetProjectBoardsLoad();
        CancelSourceProjectBoardsLoad();
        _sourceProjectBoardsLoadCts = new CancellationTokenSource();
        return _sourceProjectBoardsLoadCts;
    }

    private CancellationTokenSource BeginTargetProjectBoardsLoad()
    {
        CancelTargetProjectBoardsLoad();
        _targetProjectBoardsLoadCts = new CancellationTokenSource();
        return _targetProjectBoardsLoadCts;
    }

    private void CancelBoardLoads()
    {
        CancelSourceProjectBoardsLoad();
        CancelTargetProjectBoardsLoad();
    }

    private void CancelSourceProjectBoardsLoad()
    {
        _sourceProjectBoardsLoadCts?.Cancel();
        _sourceProjectBoardsLoadCts?.Dispose();
        _sourceProjectBoardsLoadCts = null;
    }

    private void CancelTargetProjectBoardsLoad()
    {
        _targetProjectBoardsLoadCts?.Cancel();
        _targetProjectBoardsLoadCts?.Dispose();
        _targetProjectBoardsLoadCts = null;
    }

    private bool IsStaleSourceProjectBoardsLoad(CancellationTokenSource loadCts, string expectedSourceRepositoryFullName)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_sourceProjectBoardsLoadCts, loadCts)
            || !string.Equals(sourceRepositoryFullName, expectedSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase);

    private bool IsStaleTargetProjectBoardsLoad(CancellationTokenSource loadCts, string targetRepositoryFullName)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_targetProjectBoardsLoadCts, loadCts)
            || !targetRepositoryFullNames.Contains(targetRepositoryFullName);

    private bool IsStaleTargetProjectBoardsBatch(CancellationTokenSource loadCts)
        => loadCts.IsCancellationRequested || !ReferenceEquals(_targetProjectBoardsLoadCts, loadCts);

    private sealed record ConflictOption(MigrationConflictStrategy Value, string Label, string Description);

    private sealed record RepositoryCoordinates(string Owner, string Name);
}
