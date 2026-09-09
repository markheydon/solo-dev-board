using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Components.Features.Labels.Components;
using SoloDevBoard.App.Components.Features.Labels.Dialogs;
using SoloDevBoard.App.Feedback;
using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.Labels.Pages;

/// <summary>Displays a consolidated view of labels across selected repositories.</summary>
public partial class Labels : ComponentBase
{
    private const int LabelsTabIndex = 0;

    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the application service used to retrieve labels.</summary>
    [Inject]
    public ILabelManagerService LabelManagerService { get; set; } = default!;

    /// <summary>Gets or sets the logger for label page diagnostics.</summary>
    [Inject]
    public ILogger<Labels> Logger { get; set; } = default!;

    /// <summary>Gets or sets the MudBlazor dialog service for label operations.</summary>
    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    /// <summary>Gets or sets the MudBlazor snackbar service for user feedback.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private IReadOnlyList<RepositoryDto> selectedRepositories = [];
    private IReadOnlyList<LabelMatrixRowDto> rows = [];
    private IReadOnlyList<LabelMatrixRowDto> filteredRows = [];
    private bool isLoadingRepositories = true;
    private bool isLoadingLabels;
    private bool hasLoadedLabels;
    private bool hasRepositoryLoadFailure;
    private string? errorMessage;
    private IReadOnlyList<RecommendedLabelStrategyDto> recommendedStrategies = [];
    private string selectedStrategyId = string.Empty;
    private IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto> recommendedPreview = [];
    private IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> recommendedApplyResults = [];
    private IReadOnlyList<LabelRemapResultDto> recommendedRemapResults = [];
    private Dictionary<string, Dictionary<string, LabelRemapRowState>> recommendedRemapRowsByRepository = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> recommendedStrategyLabelNames = [];
    private bool showRecommendedPreview;
    private bool removeLabelsOutsideTaxonomy;
    private bool keepAreaLabels = true;
    private bool isPreviewingRecommendedTaxonomy;
    private string taxonomyProgressDetail = string.Empty;
    private bool isApplyingRecommendedTaxonomy;
    private string syncSourceRepositoryFullName = string.Empty;
    private HashSet<string> syncTargetRepositoryFullNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<LabelSyncRepositoryPreviewDto> syncPreviewResults = [];
    private IReadOnlyList<LabelSyncRepositoryResultDto> syncApplyResults = [];
    private bool showSyncPreview;
    private bool isPreviewingSync;
    private bool isApplyingSync;
    private int activeTabIndex = LabelsTabIndex;
    private HashSet<LabelMatrixRowDto> selectedLabelRows = [];
    private bool isBulkDeletingLabels;
    private bool isAwaitingBulkDeleteConfirmation;
    private bool isReloadingFromGitHub;

    private void ShowSnackbarFeedback(string message, Severity severity)
        => SnackbarFeedback.Show(Snackbar, message, severity);

    protected override async Task OnInitializedAsync()
    {
        await LoadRecommendedStrategiesAsync();
        await LoadRepositoriesAsync();
    }

    private async Task LoadRecommendedStrategiesAsync()
    {
        try
        {
            recommendedStrategies = (await LabelManagerService.GetRecommendedLabelStrategiesAsync())
                .OrderBy(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedStrategyId = recommendedStrategies
                .FirstOrDefault(strategy => strategy.Id.Equals("solodevboard", StringComparison.OrdinalIgnoreCase))?.Id
                ?? recommendedStrategies.FirstOrDefault()?.Id
                ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load recommended label strategies.");
            recommendedStrategies = [];
            selectedStrategyId = string.Empty;
            ShowSnackbarFeedback("Unable to load recommended taxonomy strategies.", Severity.Error);
        }
    }

    private async Task ReloadRepositoriesAsync()
    {
        await RetryLoadRepositoriesAsync();
    }

    private async Task ReloadFromGitHubAsync()
    {
        if (isLoadingRepositories || isReloadingFromGitHub || IsPageWriteBusy)
        {
            return;
        }

        var preservedRepositoryFullNames = selectedRepositoryFullNames.ToArray();
        var preservedStrategyId = selectedStrategyId;
        var preservedRemoveOutside = removeLabelsOutsideTaxonomy;
        var preservedKeepArea = keepAreaLabels;
        var preservedSyncSource = syncSourceRepositoryFullName;
        var preservedSyncTargets = syncTargetRepositoryFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shouldReloadLabels = hasLoadedLabels && preservedRepositoryFullNames.Length > 0;

        isReloadingFromGitHub = true;

        try
        {
            await RefreshRepositoriesCatalogueAsync(forceReload: true);

            if (preservedRepositoryFullNames.Length > 0)
            {
                var preservedNames = preservedRepositoryFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
                selectedRepositories = availableRepositories
                    .Where(repository => preservedNames.Contains(repository.FullName))
                    .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                selectedStrategyId = recommendedStrategies.Any(strategy => strategy.Id.Equals(preservedStrategyId, StringComparison.OrdinalIgnoreCase))
                    ? preservedStrategyId
                    : selectedStrategyId;

                removeLabelsOutsideTaxonomy = preservedRemoveOutside;
                keepAreaLabels = preservedKeepArea;
                syncSourceRepositoryFullName = preservedSyncSource;
                syncTargetRepositoryFullNames = preservedSyncTargets
                    .Where(preservedNames.Contains)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                EnsureSyncSelections();
            }

            if (shouldReloadLabels && string.IsNullOrWhiteSpace(errorMessage))
            {
                await LoadLabelsForSelectionAsync(forceReload: true);
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

    private async Task RetryLoadLabelsAsync()
    {
        await LoadLabelsForSelectionAsync(forceReload: true);
    }

    private async Task LoadSelectedLabelsAsync()
    {
        await LoadLabelsForSelectionAsync();
    }

    private Task OnActiveTabIndexChanged(int tabIndex)
    {
        activeTabIndex = tabIndex;
        return Task.CompletedTask;
    }

    private Task OnStrategySelectedAsync(string strategyId)
    {
        selectedStrategyId = strategyId;
        ClearRecommendedTaxonomyWorkflow();
        return Task.CompletedTask;
    }

    private Task OnRemoveLabelsOutsideTaxonomyChanged(bool value)
    {
        removeLabelsOutsideTaxonomy = value;
        ClearRecommendedTaxonomyWorkflow();
        return Task.CompletedTask;
    }

    private Task OnKeepAreaLabelsChanged(bool value)
    {
        keepAreaLabels = value;
        ClearRecommendedTaxonomyWorkflow();
        syncPreviewResults = [];
        showSyncPreview = false;
        syncApplyResults = [];
        return Task.CompletedTask;
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        errorMessage = null;
        rows = [];
        filteredRows = [];
        hasLoadedLabels = false;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedRepositories = [];
            recommendedPreview = [];
            recommendedApplyResults = [];
            showRecommendedPreview = false;
            ResetSyncWorkflow();
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
            hasRepositoryLoadFailure = true;
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            hasRepositoryLoadFailure = true;
            Logger.LogError(ex, "Failed to load repositories.");
            errorMessage = "An unexpected error occurred while loading repositories.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private async Task RefreshRepositoriesCatalogueAsync(bool forceReload)
    {
        hasRepositoryLoadFailure = false;
        errorMessage = null;

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
            hasRepositoryLoadFailure = true;
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            hasRepositoryLoadFailure = true;
            Logger.LogError(ex, "Failed to refresh repositories for the Label Manager.");
            errorMessage = "An unexpected error occurred while refreshing repositories.";
        }
    }

    private Task OnSelectedRepositoriesChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var selectedNames = repositoryFullNames
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        selectedRepositories = availableRepositories
            .Where(repository => selectedNames.Contains(repository.FullName))
            .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ClearRecommendedTaxonomyWorkflow();
        EnsureSyncSelections();
        return Task.CompletedTask;
    }

    private Task OnSyncSourceRepositoryChangedAsync(string value)
    {
        syncSourceRepositoryFullName = value ?? string.Empty;
        syncTargetRepositoryFullNames.Remove(syncSourceRepositoryFullName);
        syncPreviewResults = [];
        showSyncPreview = false;
        syncApplyResults = [];
        return Task.CompletedTask;
    }

    private Task OnSyncTargetRepositoryChangedAsync(string repositoryFullName, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(repositoryFullName) || repositoryFullName.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (isSelected)
        {
            _ = syncTargetRepositoryFullNames.Add(repositoryFullName);
        }
        else
        {
            _ = syncTargetRepositoryFullNames.Remove(repositoryFullName);
        }

        syncPreviewResults = [];
        showSyncPreview = false;
        syncApplyResults = [];
        return Task.CompletedTask;
    }

    private async Task PreviewRecommendedTaxonomyAsync()
    {
        if (isPreviewingRecommendedTaxonomy)
        {
            return;
        }

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before previewing taxonomy changes.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedStrategyId))
        {
            ShowSnackbarFeedback("Select a recommended strategy before previewing taxonomy changes.", Severity.Warning);
            return;
        }

        isPreviewingRecommendedTaxonomy = true;
        taxonomyProgressDetail = "Previewing taxonomy changes...";
        await InvokeAsync(StateHasChanged);

        var progress = new Progress<string>(message =>
        {
            taxonomyProgressDetail = message;
            _ = InvokeAsync(StateHasChanged);
        });

        try
        {
            recommendedApplyResults = [];
            recommendedRemapResults = [];
            recommendedPreview = await LabelManagerService.PreviewRecommendedTaxonomyAsync(
                selectedStrategyId,
                selectedFullNames,
                removeLabelsOutsideTaxonomy,
                keepAreaLabels,
                progress);
            recommendedStrategyLabelNames = ResolveStrategyLabelNames();
            InitialiseRemapRows(recommendedPreview);
            showRecommendedPreview = true;
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
            Logger.LogError(ex, "GitHub API request failed while previewing strategy {StrategyId}.", selectedStrategyId);
            ShowSnackbarFeedback($"GitHub API request failed while previewing taxonomy changes. {ex.Message}", Severity.Error);
            showRecommendedPreview = false;
            recommendedPreview = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview strategy {StrategyId}.", selectedStrategyId);
            ShowSnackbarFeedback("An unexpected error occurred while previewing taxonomy changes.", Severity.Error);
            showRecommendedPreview = false;
            recommendedPreview = [];
        }
        finally
        {
            isPreviewingRecommendedTaxonomy = false;
            taxonomyProgressDetail = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CancelRecommendedPreview()
    {
        showRecommendedPreview = false;
        recommendedPreview = [];
        recommendedRemapRowsByRepository = new Dictionary<string, Dictionary<string, LabelRemapRowState>>(StringComparer.OrdinalIgnoreCase);
        ShowSnackbarFeedback("Taxonomy apply was cancelled.", Severity.Info);
    }

    private async Task ApplyRecommendedTaxonomyAsync()
    {
        if (isApplyingRecommendedTaxonomy)
        {
            return;
        }

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before applying taxonomy changes.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedStrategyId))
        {
            ShowSnackbarFeedback("Select a recommended strategy before applying taxonomy changes.", Severity.Warning);
            return;
        }

        if (removeLabelsOutsideTaxonomy && HasRemapExtras() && !await ConfirmDeleteWithoutRemapAsync())
        {
            return;
        }

        isApplyingRecommendedTaxonomy = true;
        taxonomyProgressDetail = "Applying taxonomy changes...";
        await InvokeAsync(StateHasChanged);

        var progress = new Progress<string>(message =>
        {
            taxonomyProgressDetail = message;
            _ = InvokeAsync(StateHasChanged);
        });

        try
        {
            if (removeLabelsOutsideTaxonomy && HasRemapExtras())
            {
                recommendedApplyResults = await LabelManagerService.ApplyRecommendedTaxonomyAsync(
                    selectedStrategyId,
                    selectedFullNames,
                    false,
                    keepAreaLabels,
                    progress);
                recommendedRemapResults = await ApplyRemapPlanAsync(recommendedPreview, progress);
                var unusedDeletedByRepository = await DeleteUnusedExtraLabelsAsync(recommendedPreview, progress);
                recommendedApplyResults = RecommendedTaxonomyApplySummaryHelper.EnrichDeletedCounts(
                    recommendedApplyResults,
                    recommendedRemapResults,
                    unusedDeletedByRepository);
            }
            else
            {
                recommendedApplyResults = await LabelManagerService.ApplyRecommendedTaxonomyAsync(
                    selectedStrategyId,
                    selectedFullNames,
                    removeLabelsOutsideTaxonomy,
                    keepAreaLabels,
                    progress);
                recommendedRemapResults = [];
            }

            showRecommendedPreview = false;
            recommendedPreview = [];
            recommendedRemapRowsByRepository = new Dictionary<string, Dictionary<string, LabelRemapRowState>>(StringComparer.OrdinalIgnoreCase);

            var failedCount = recommendedApplyResults.Count(result => result.HasError);
            var createdCount = recommendedApplyResults.Sum(result => result.CreatedCount);
            var updatedCount = recommendedApplyResults.Sum(result => result.UpdatedCount);
            var deletedCount = recommendedApplyResults.Sum(result => result.DeletedCount);
            var skippedCount = recommendedApplyResults.Sum(result => result.SkippedCount);
            var remappedCount = recommendedRemapResults.Count(result => result.SourceDeleted);
            var remapFailureCount = recommendedRemapResults.Count(result => result.HasErrors);

            if (failedCount == 0 && remapFailureCount == 0)
            {
                var message = remappedCount > 0
                    ? $"Applied taxonomy successfully. Created {createdCount}, updated {updatedCount}, remapped {remappedCount}, deleted {deletedCount}, skipped {skippedCount}."
                    : $"Applied taxonomy successfully. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.";
                ShowSnackbarFeedback(message, Severity.Success);
            }
            else
            {
                var message = remappedCount > 0 || remapFailureCount > 0
                    ? $"Applied taxonomy with {failedCount + remapFailureCount} error(s). Created {createdCount}, updated {updatedCount}, remapped {remappedCount}, deleted {deletedCount}, skipped {skippedCount}."
                    : $"Applied taxonomy with {failedCount} repository errors. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.";
                ShowSnackbarFeedback(message, Severity.Warning);
            }

            await LoadLabelsForSelectionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply strategy {StrategyId}.", selectedStrategyId);
            ShowSnackbarFeedback("An unexpected error occurred while applying taxonomy changes.", Severity.Error);
        }
        finally
        {
            isApplyingRecommendedTaxonomy = false;
            taxonomyProgressDetail = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadLabelsForSelectionAsync(bool forceReload = false)
    {
        hasRepositoryLoadFailure = false;
        errorMessage = null;
        hasLoadedLabels = true;

        var selectedRepositoryFullNames = selectedRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository.FullName))
            .Select(repository => repository.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedRepositoryFullNames.Length == 0)
        {
            rows = [];
            ApplyFilter();
            return;
        }

        isLoadingLabels = true;

        try
        {
            rows = await LabelManagerService.GetLabelMatrixAsync(selectedRepositoryFullNames, forceReload: forceReload);
            ApplyFilter();
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
            rows = [];
            filteredRows = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load labels for selected repositories.");
            errorMessage = "An unexpected error occurred while loading labels.";
            rows = [];
            filteredRows = [];
        }
        finally
        {
            isLoadingLabels = false;
        }
    }

    private async Task PreviewLabelSynchronisationAsync()
    {
        if (isPreviewingSync)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(syncSourceRepositoryFullName))
        {
            ShowSnackbarFeedback("Select a source repository before previewing label synchronisation.", Severity.Warning);
            return;
        }

        var targets = syncTargetRepositoryFullNames
            .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (targets.Length == 0)
        {
            ShowSnackbarFeedback("Select at least one target repository before previewing label synchronisation.", Severity.Warning);
            return;
        }

        isPreviewingSync = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            syncApplyResults = [];
            syncPreviewResults = await LabelManagerService.PreviewLabelSynchronisationAsync(syncSourceRepositoryFullName, targets, keepAreaLabels);
            showSyncPreview = true;
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
            Logger.LogError(ex, "GitHub API request failed while previewing synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            ShowSnackbarFeedback($"GitHub API request failed while previewing synchronisation. {ex.Message}", Severity.Error);
            showSyncPreview = false;
            syncPreviewResults = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while previewing synchronisation.", Severity.Error);
            showSyncPreview = false;
            syncPreviewResults = [];
        }
        finally
        {
            isPreviewingSync = false;
        }
    }

    private void CancelLabelSynchronisationPreview()
    {
        showSyncPreview = false;
        syncPreviewResults = [];
        ShowSnackbarFeedback("Label synchronisation preview was cancelled. No changes were applied.", Severity.Info);
    }

    private async Task ApplyLabelSynchronisationAsync()
    {
        if (isApplyingSync)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(syncSourceRepositoryFullName))
        {
            ShowSnackbarFeedback("Select a source repository before applying label synchronisation.", Severity.Warning);
            return;
        }

        var targets = syncTargetRepositoryFullNames
            .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (targets.Length == 0)
        {
            ShowSnackbarFeedback("Select at least one target repository before applying label synchronisation.", Severity.Warning);
            return;
        }

        isApplyingSync = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            syncApplyResults = await LabelManagerService.ApplyLabelSynchronisationAsync(syncSourceRepositoryFullName, targets, keepAreaLabels);
            showSyncPreview = false;
            syncPreviewResults = [];

            var failedCount = syncApplyResults.Count(result => result.HasError);
            var createdCount = syncApplyResults.Sum(result => result.CreatedCount);
            var updatedCount = syncApplyResults.Sum(result => result.UpdatedCount);
            var deletedCount = syncApplyResults.Sum(result => result.DeletedCount);
            var skippedCount = syncApplyResults.Sum(result => result.SkippedCount);

            if (failedCount == 0)
            {
                ShowSnackbarFeedback($"Synchronisation completed. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.", Severity.Success);
            }
            else
            {
                ShowSnackbarFeedback($"Synchronisation completed with {failedCount} repository failures. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.", Severity.Warning);
            }

            await LoadLabelsForSelectionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            ShowSnackbarFeedback("An unexpected error occurred while applying synchronisation.", Severity.Error);
        }
        finally
        {
            isApplyingSync = false;
        }
    }

    private async Task ShowCreateDialogAsync()
    {
        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before creating a label.", Severity.Warning);
            return;
        }

        var request = new LabelOperationDialogRequest(
            LabelOperationMode.Create,
            string.Empty,
            string.Empty,
            "#ededed",
            string.Empty,
            selectedFullNames,
            selectedFullNames,
            selectedFullNames);

        var result = await ShowLabelOperationDialogAsync("New label", request);
        if (result is null)
        {
            return;
        }

        await ExecuteCreateAsync(result);
    }

    private async Task ShowEditDialogAsync(LabelMatrixRowDto row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before editing a label.", Severity.Warning);
            return;
        }

        var defaultSelection = row.RepositoriesWithLabel.Count > 0
            ? row.RepositoriesWithLabel
            : selectedFullNames;

        var request = new LabelOperationDialogRequest(
            LabelOperationMode.Edit,
            row.Name,
            row.Name,
            $"#{row.Colour}",
            row.Description == LabelMatrixRowDto.MissingDescriptionDisplay ? string.Empty : row.Description,
            selectedFullNames,
            row.RepositoriesWithLabel,
            defaultSelection);

        var result = await ShowLabelOperationDialogAsync("Edit label", request);
        if (result is null)
        {
            return;
        }

        await ExecuteUpdateAsync(result);
    }

    private async Task ShowDeleteDialogAsync(LabelMatrixRowDto row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before deleting a label.", Severity.Warning);
            return;
        }

        var defaultSelection = row.RepositoriesWithLabel.Count > 0
            ? row.RepositoriesWithLabel
            : selectedFullNames;

        var request = new LabelOperationDialogRequest(
            LabelOperationMode.Delete,
            row.Name,
            row.Name,
            $"#{row.Colour}",
            row.Description,
            selectedFullNames,
            row.RepositoriesWithLabel,
            defaultSelection);

        var result = await ShowLabelOperationDialogAsync("Delete label", request);
        if (result is null)
        {
            return;
        }

        await ExecuteDeleteAsync(result);
    }

    private async Task<LabelOperationDialogResult?> ShowLabelOperationDialogAsync(string title, LabelOperationDialogRequest request)
    {
        var parameters = new DialogParameters<LabelOperationDialog>
        {
            { dialog => dialog.Content, request },
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            BackdropClick = false,
            CloseOnEscapeKey = true,
        };

        var dialog = await DialogService.ShowAsync<LabelOperationDialog>(title, parameters, options);

        var dialogResult = await dialog.Result;
        if (dialogResult is null)
        {
            return null;
        }

        if (dialogResult.Canceled)
        {
            return null;
        }

        if (dialogResult.Data is not LabelOperationDialogResult result)
        {
            Logger.LogWarning("Label operation dialog closed without a valid result payload for {Mode}.", request.Mode);
            ShowSnackbarFeedback("No changes were saved. Please use the form action button in the dialog.", Severity.Warning);
            return null;
        }

        return result;
    }

    private async Task ExecuteCreateAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = RepositoryFullName.GroupByOwner(operation.SelectedRepositories);
        var createRequest = new LabelDto(operation.LabelName, operation.Colour, operation.Description, string.Empty);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                var created = await LabelManagerService.CreateLabelAsync(ownerGroup.Key, ownerGroup.Value, createRequest);
                changedRepositoryCount += created.Count;
            }

            ShowSnackbarFeedback($"Created '{operation.LabelName}' in {changedRepositoryCount} repositories.", Severity.Success);
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
            Logger.LogError(ex, "GitHub API request failed while creating label {LabelName}.", operation.LabelName);
            ShowSnackbarFeedback($"GitHub API request failed while creating '{operation.LabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create label {LabelName}.", operation.LabelName);
            ShowSnackbarFeedback($"An unexpected error occurred while creating '{operation.LabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private async Task ExecuteUpdateAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = RepositoryFullName.GroupByOwner(operation.SelectedRepositories);
        var updateRequest = new LabelDto(operation.LabelName, operation.Colour, operation.Description, string.Empty);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                var updated = await LabelManagerService.UpdateLabelAsync(ownerGroup.Key, ownerGroup.Value, operation.OriginalLabelName, updateRequest);
                changedRepositoryCount += updated.Count;
            }

            ShowSnackbarFeedback($"Updated '{operation.OriginalLabelName}' across {changedRepositoryCount} repositories.", Severity.Success);
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
            Logger.LogError(ex, "GitHub API request failed while updating label {LabelName}.", operation.OriginalLabelName);
            ShowSnackbarFeedback($"GitHub API request failed while updating '{operation.OriginalLabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update label {LabelName}.", operation.OriginalLabelName);
            ShowSnackbarFeedback($"An unexpected error occurred while updating '{operation.OriginalLabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private async Task ExecuteDeleteAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = RepositoryFullName.GroupByOwner(operation.SelectedRepositories);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                await LabelManagerService.DeleteLabelAsync(ownerGroup.Key, ownerGroup.Value, operation.OriginalLabelName);
                changedRepositoryCount += ownerGroup.Value.Count;
            }

            ShowSnackbarFeedback($"Deleted '{operation.OriginalLabelName}' from {changedRepositoryCount} repositories.", Severity.Success);
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
            Logger.LogError(ex, "GitHub API request failed while deleting label {LabelName}.", operation.OriginalLabelName);
            ShowSnackbarFeedback($"GitHub API request failed while deleting '{operation.OriginalLabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete label {LabelName}.", operation.OriginalLabelName);
            ShowSnackbarFeedback($"An unexpected error occurred while deleting '{operation.OriginalLabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private Task OnSelectedLabelRowsChangedAsync(ICollection<LabelMatrixRowDto> selectedRows)
    {
        selectedLabelRows = selectedRows.ToHashSet();
        return Task.CompletedTask;
    }

    private async Task ShowBulkDeleteConfirmDialogAsync()
    {
        if (isBulkDeletingLabels || isAwaitingBulkDeleteConfirmation || selectedLabelRows.Count == 0)
        {
            return;
        }

        if (!TryBuildBulkDeleteTargets(out var targets))
        {
            return;
        }

        var dialogTargets = targets
            .Select(target => new LabelBulkDeleteConfirmDialogLabelTarget(
                target.LabelName,
                target.RepositoryFullNames))
            .ToArray();

        var request = new LabelBulkDeleteConfirmDialogRequest(dialogTargets);
        var parameters = new DialogParameters<LabelBulkDeleteConfirmDialog>
        {
            { dialog => dialog.Content, request },
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            BackdropClick = false,
            CloseOnEscapeKey = true,
        };

        isAwaitingBulkDeleteConfirmation = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var dialog = await DialogService.ShowAsync<LabelBulkDeleteConfirmDialog>("Delete labels", parameters, options);
            var dialogResult = await dialog.Result;
            if (dialogResult is null || dialogResult.Canceled)
            {
                return;
            }

            await ExecuteBulkDeleteAsync(targets);
        }
        finally
        {
            isAwaitingBulkDeleteConfirmation = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ExecuteBulkDeleteAsync(LabelBulkDeleteTargetDto[] targets)
    {
        if (isBulkDeletingLabels || targets.Length == 0)
        {
            return;
        }

        isBulkDeletingLabels = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await LabelManagerService.BulkDeleteLabelsAsync(targets);

            if (!result.HasErrors)
            {
                ShowSnackbarFeedback(FormatBulkDeleteSummaryMessage(result.DeletedCount, result.SkippedCount, 0), Severity.Success);
            }
            else
            {
                ShowSnackbarFeedback(
                    FormatBulkDeleteSummaryMessage(result.DeletedCount, result.SkippedCount, result.Errors.Count),
                    Severity.Warning);

                foreach (var error in result.Errors.Take(5))
                {
                    ShowSnackbarFeedback(
                        $"Failed to delete '{error.LabelName}' from {error.RepositoryFullName}: {error.ErrorMessage}",
                        Severity.Error);
                }

                if (result.Errors.Count > 5)
                {
                    ShowSnackbarFeedback($"{result.Errors.Count - 5} additional delete failures were not shown.", Severity.Error);
                }
            }
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
            Logger.LogError(ex, "Failed to bulk delete labels.");
            ShowSnackbarFeedback("An unexpected error occurred while deleting labels.", Severity.Error);
        }
        finally
        {
            isBulkDeletingLabels = false;
            selectedLabelRows = [];
            await LoadLabelsForSelectionAsync();
        }
    }

    private bool TryBuildBulkDeleteTargets(out LabelBulkDeleteTargetDto[] targets)
    {
        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ShowSnackbarFeedback("Select at least one repository before deleting labels.", Severity.Warning);
            targets = [];
            return false;
        }

        var selectedRepositorySet = selectedFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        targets = selectedLabelRows
            .Select(row => new LabelBulkDeleteTargetDto(
                row.Name,
                row.RepositoriesWithLabel
                    .Where(repositoryFullName => selectedRepositorySet.Contains(repositoryFullName))
                    .OrderBy(repositoryFullName => repositoryFullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .Where(target => target.RepositoryFullNames.Count > 0)
            .OrderBy(target => target.LabelName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (targets.Length == 0)
        {
            ShowSnackbarFeedback("None of the selected labels are present in the selected repositories.", Severity.Warning);
            return false;
        }

        return true;
    }

    private static string FormatBulkDeleteSummaryMessage(int deletedCount, int skippedCount, int failureCount)
    {
        var summary = $"Deleted {deletedCount} label-repository pair{(deletedCount == 1 ? string.Empty : "s")}";

        if (skippedCount > 0)
        {
            summary += $" ({skippedCount} skipped)";
        }

        if (failureCount > 0)
        {
            return $"{summary} with {failureCount} failure{(failureCount == 1 ? string.Empty : "s")}.";
        }

        return $"{summary}.";
    }

    private bool TryGetSelectedRepositoryFullNames(out IReadOnlyList<string> selectedFullNames)
    {
        selectedFullNames = selectedRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository.FullName))
            .Select(repository => repository.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return selectedFullNames.Count > 0;
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            filteredRows = rows;
            return;
        }

        filteredRows = rows
            .Where(row => row.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private string _searchText = string.Empty;

    private string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            ApplyFilter();
        }
    }

    private bool ShowLoadingState => isLoadingRepositories || isLoadingLabels;

    private bool IsPageWriteBusy => isPreviewingRecommendedTaxonomy
        || isApplyingRecommendedTaxonomy
        || isPreviewingSync
        || isApplyingSync
        || isBulkDeletingLabels
        || isAwaitingBulkDeleteConfirmation;

    private bool ShowReloadFromGitHubButton => !isLoadingRepositories;

    private bool IsReloadFromGitHubDisabled => isReloadingFromGitHub || IsPageWriteBusy;

    private bool IsRecommendedTaxonomyBusy => isPreviewingRecommendedTaxonomy || isApplyingRecommendedTaxonomy;

    private bool IsSynchronisationBusy => isPreviewingSync || isApplyingSync;

    private bool IsLabelsTabBusy => isBulkDeletingLabels || isAwaitingBulkDeleteConfirmation;

    private string TaxonomyProgressAriaLabel => isApplyingRecommendedTaxonomy
        ? "Applying taxonomy changes"
        : "Previewing taxonomy changes";

    private string TaxonomyProgressMessage
        => string.IsNullOrWhiteSpace(taxonomyProgressDetail)
            ? isApplyingRecommendedTaxonomy
                ? "Applying taxonomy changes. Duplicate submissions are disabled."
                : "Previewing taxonomy changes..."
            : taxonomyProgressDetail;

    private string SyncProgressAriaLabel => isApplyingSync
        ? "Applying synchronisation changes"
        : "Previewing synchronisation";

    private string SyncProgressMessage => isApplyingSync
        ? "Applying synchronisation changes. Duplicate submissions are disabled."
        : "Previewing synchronisation...";

    private bool CanPreviewRecommendedTaxonomy => !ShowLoadingState
        && !IsRecommendedTaxonomyBusy
        && selectedRepositories.Count > 0
        && !string.IsNullOrWhiteSpace(selectedStrategyId);

    private bool CanApplyRecommendedTaxonomy => showRecommendedPreview
        && (recommendedPreview.Any(HasRecommendedTaxonomyActions) || HasRemapActions())
        && !isApplyingRecommendedTaxonomy;

    private bool UsesRemapWorkflow => removeLabelsOutsideTaxonomy && HasRemapExtras();

    private bool CanPreviewLabelSynchronisation => !ShowLoadingState
        && !IsSynchronisationBusy
        && !string.IsNullOrWhiteSpace(syncSourceRepositoryFullName)
        && syncTargetRepositoryFullNames.Count > 0;

    private bool CanApplyLabelSynchronisation => showSyncPreview
        && syncPreviewResults.Any(HasLabelSynchronisationActions)
        && !isApplyingSync;

    private static bool HasRecommendedTaxonomyActions(RecommendedTaxonomyRepositoryPreviewDto preview)
        => preview.ToCreate.Count > 0
            || preview.ToUpdate.Count > 0
            || preview.ToDelete.Count > 0
            || preview.ToRemap.Count > 0;

    private bool HasRemapExtras()
        => recommendedPreview.Any(preview => preview.ToRemap.Count > 0);

    private bool HasRemapActions()
        => recommendedRemapRowsByRepository.Values
            .SelectMany(rows => rows.Values)
            .Any(row => row.DeleteWithoutRemap || !string.IsNullOrWhiteSpace(row.DestinationName));

    private static bool HasLabelSynchronisationActions(LabelSyncRepositoryPreviewDto preview)
        => preview.ToCreate.Count > 0
            || preview.ToUpdate.Count > 0
            || preview.ToDelete.Count > 0;

    private bool CanBulkDeleteLabels => hasLoadedLabels
        && selectedLabelRows.Count > 0
        && selectedRepositories.Count > 0
        && !IsLabelsTabBusy
        && !isLoadingLabels;

    private string BulkDeleteProgressAriaLabel => "Deleting selected labels";

    private string BulkDeleteProgressMessage => "Deleting selected labels. Duplicate submissions are disabled.";

    private string SelectedStrategyDescription
        => recommendedStrategies.FirstOrDefault(strategy => strategy.Id.Equals(selectedStrategyId, StringComparison.OrdinalIgnoreCase))?.Description
            ?? string.Empty;

    private static string FormatPreviewActionCounts(int createCount, int updateCount, int deleteCount, int skipCount, int remapCount = 0)
    {
        if (remapCount > 0)
        {
            return deleteCount > 0
                ? $"Create: {createCount}, Update: {updateCount}, Remap: {remapCount}, Delete: {deleteCount}, Skip: {skipCount}"
                : $"Create: {createCount}, Update: {updateCount}, Remap: {remapCount}, Skip: {skipCount}";
        }

        return $"Create: {createCount}, Update: {updateCount}, Delete: {deleteCount}, Skip: {skipCount}";
    }

    private bool ShowLabelFilter => hasLoadedLabels && rows.Count > 0 && !ShowLoadingState && string.IsNullOrWhiteSpace(errorMessage);

    private string ErrorTitle => hasRepositoryLoadFailure
        ? "Unable to load repositories"
        : "Unable to load labels";

    private string RepositorySelectorSummary
    {
        get
        {
            var repositoryCount = availableRepositories.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {selectedRepositories.Count} selected. Archived repositories are hidden by default.";
        }
    }

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

    private static string GetColourChipStyle(string colour) => LabelColourStyleHelper.GetColourChipStyle(colour);

    private void EnsureSyncSelections()
    {
        var selectedRepositoryNames = selectedRepositories
            .Select(repository => repository.FullName)
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedRepositoryNames.Count == 0)
        {
            ResetSyncWorkflow();
            return;
        }

        if (string.IsNullOrWhiteSpace(syncSourceRepositoryFullName) || !selectedRepositoryNames.Contains(syncSourceRepositoryFullName))
        {
            syncSourceRepositoryFullName = selectedRepositoryNames
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        syncTargetRepositoryFullNames = syncTargetRepositoryFullNames
            .Where(selectedRepositoryNames.Contains)
            .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (syncTargetRepositoryFullNames.Count == 0)
        {
            syncTargetRepositoryFullNames = selectedRepositoryNames
                .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        syncPreviewResults = [];
        showSyncPreview = false;
        syncApplyResults = [];
    }

    private void ResetSyncWorkflow()
    {
        syncSourceRepositoryFullName = string.Empty;
        syncTargetRepositoryFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        syncPreviewResults = [];
        syncApplyResults = [];
        showSyncPreview = false;
        isPreviewingSync = false;
        isApplyingSync = false;
    }

    private void ClearRecommendedTaxonomyWorkflow()
    {
        recommendedPreview = [];
        showRecommendedPreview = false;
        recommendedApplyResults = [];
        recommendedRemapResults = [];
        recommendedRemapRowsByRepository = new Dictionary<string, Dictionary<string, LabelRemapRowState>>(StringComparer.OrdinalIgnoreCase);
        recommendedStrategyLabelNames = [];
    }

    private IReadOnlyList<string> ResolveStrategyLabelNames()
    {
        if (RecommendedLabelTaxonomyCatalog.TryGetLabels(selectedStrategyId, out var labels))
        {
            return labels.Select(label => label.Name).ToArray();
        }

        return [];
    }

    private void InitialiseRemapRows(IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto> previews)
    {
        recommendedRemapRowsByRepository = new Dictionary<string, Dictionary<string, LabelRemapRowState>>(StringComparer.OrdinalIgnoreCase);

        foreach (var preview in previews)
        {
            if (preview.ToRemap.Count == 0)
            {
                continue;
            }

            var rows = new Dictionary<string, LabelRemapRowState>(StringComparer.OrdinalIgnoreCase);

            foreach (var extra in preview.ToRemap)
            {
                var suggestedDestination = LabelRemapSuggestionHelper.SuggestDestination(extra.Name, recommendedStrategyLabelNames);
                rows[extra.Name] = new LabelRemapRowState(extra.Name, suggestedDestination, false);
            }

            recommendedRemapRowsByRepository[preview.RepositoryFullName] = rows;
        }
    }

    private static IReadOnlyList<string> CollectSurvivingLabelNames(RecommendedTaxonomyRepositoryPreviewDto preview)
        => preview.ToCreate
            .Concat(preview.ToUpdate)
            .Concat(preview.Skipped)
            .Select(label => label.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> CollectOutgoingLabelNames(RecommendedTaxonomyRepositoryPreviewDto preview)
        => preview.ToRemap
            .Select(label => label.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<LabelRemapRowState> GetRemapRows(string repositoryFullName)
    {
        if (!recommendedRemapRowsByRepository.TryGetValue(repositoryFullName, out var rows))
        {
            return [];
        }

        return rows.Values
            .OrderBy(row => row.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> GetRemapDestinationOptions(string repositoryFullName, string sourceLabelName)
    {
        var preview = recommendedPreview.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase));
        if (preview is null)
        {
            return [];
        }

        return LabelRemapSuggestionHelper.BuildDestinationOptions(
            sourceLabelName,
            recommendedStrategyLabelNames,
            CollectSurvivingLabelNames(preview),
            CollectOutgoingLabelNames(preview));
    }

    private Task<IEnumerable<string>> SearchRemapDestinationOptionsAsync(
        string repositoryFullName,
        string sourceLabelName,
        string? value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = GetRemapDestinationOptions(repositoryFullName, sourceLabelName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string>>(options);
        }

        var filter = value.Trim();
        return Task.FromResult(options.Where(option => option.Contains(filter, StringComparison.OrdinalIgnoreCase)));
    }

    private static string FormatRemapDestinationValue(string? destinationName)
        => destinationName ?? string.Empty;

    private Task OnRemapDestinationChangedAsync(string repositoryFullName, string sourceLabelName, string? destinationName)
    {
        if (!recommendedRemapRowsByRepository.TryGetValue(repositoryFullName, out var rows)
            || !rows.TryGetValue(sourceLabelName, out var row))
        {
            return Task.CompletedTask;
        }

        rows[sourceLabelName] = row with
        {
            DestinationName = string.IsNullOrWhiteSpace(destinationName) ? null : destinationName,
            DeleteWithoutRemap = false,
        };
        return Task.CompletedTask;
    }

    private Task OnRemapDeleteWithoutRemapAsync(string repositoryFullName, string sourceLabelName)
    {
        if (!recommendedRemapRowsByRepository.TryGetValue(repositoryFullName, out var rows)
            || !rows.TryGetValue(sourceLabelName, out var row))
        {
            return Task.CompletedTask;
        }

        rows[sourceLabelName] = row with { DestinationName = null, DeleteWithoutRemap = true };
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyDictionary<string, int>> DeleteUnusedExtraLabelsAsync(
        IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto> previews,
        IProgress<string>? progress)
    {
        var deletedByRepository = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var deleteActions = previews
            .Where(preview => preview.ToDelete.Count > 0)
            .SelectMany(preview => preview.ToDelete.Select(label => (preview.RepositoryFullName, Label: label)))
            .ToArray();

        for (var actionIndex = 0; actionIndex < deleteActions.Length; actionIndex++)
        {
            var (repositoryFullName, label) = deleteActions[actionIndex];
            ReportTaxonomyProgress(
                progress,
                deleteActions.Length > 1
                    ? $"Deleting unused label '{label.Name}' on {repositoryFullName} ({actionIndex + 1} of {deleteActions.Length})..."
                    : $"Deleting unused label '{label.Name}' on {repositoryFullName}...");

            if (!RepositoryFullName.TryParse(repositoryFullName, out var owner, out var repositoryName))
            {
                continue;
            }

            try
            {
                await LabelManagerService.DeleteLabelAsync(owner, [repositoryName], label.Name);
                deletedByRepository[repositoryFullName] = deletedByRepository.GetValueOrDefault(repositoryFullName) + 1;
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                Logger.LogWarning(
                    ex,
                    "Failed to delete unused extra label {LabelName} from {RepositoryFullName}.",
                    label.Name,
                    repositoryFullName);
            }
        }

        return deletedByRepository;
    }

    private async Task<bool> ConfirmDeleteWithoutRemapAsync()
    {
        var deleteTargets = recommendedRemapRowsByRepository
            .SelectMany(pair => pair.Value.Values
                .Where(row => row.DeleteWithoutRemap)
                .Select(row => $"{pair.Key}: {row.SourceName}"))
            .OrderBy(target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (deleteTargets.Length == 0)
        {
            return true;
        }

        var message = "The following labels will be deleted without remapping issue or pull request history:\n\n"
            + string.Join('\n', deleteTargets)
            + "\n\nContinue?";

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete without remap",
            message,
            yesText: "Delete",
            cancelText: "Cancel");

        return confirmed == true;
    }

    private async Task<IReadOnlyList<LabelRemapResultDto>> ApplyRemapPlanAsync(
        IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto> previews,
        IProgress<string>? progress)
    {
        var results = new List<LabelRemapResultDto>();
        var plannedActions = previews
            .Where(preview => recommendedRemapRowsByRepository.TryGetValue(preview.RepositoryFullName, out _))
            .SelectMany(preview => recommendedRemapRowsByRepository[preview.RepositoryFullName].Values
                .Where(row => row.DeleteWithoutRemap || !string.IsNullOrWhiteSpace(row.DestinationName))
                .Select(row => (Preview: preview, Row: row)))
            .OrderBy(action => action.Preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(action => action.Row.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ReportTaxonomyProgress(progress, "Applying remap plan...");

        for (var actionIndex = 0; actionIndex < plannedActions.Length; actionIndex++)
        {
            var (preview, row) = plannedActions[actionIndex];
            if (!RepositoryFullName.TryParse(preview.RepositoryFullName, out var owner, out var repositoryName))
            {
                continue;
            }

            if (row.DeleteWithoutRemap)
            {
                ReportTaxonomyProgress(
                    progress,
                    plannedActions.Length > 1
                        ? $"Deleting '{row.SourceName}' without remap on {preview.RepositoryFullName} ({actionIndex + 1} of {plannedActions.Length})..."
                        : $"Deleting '{row.SourceName}' without remap on {preview.RepositoryFullName}...");

                try
                {
                    await LabelManagerService.DeleteLabelAsync(owner, [repositoryName], row.SourceName);
                    results.Add(new LabelRemapResultDto(
                        repositoryName,
                        row.SourceName,
                        string.Empty,
                        0,
                        0,
                        false,
                        false,
                        []));
                }
                catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
                {
                    results.Add(new LabelRemapResultDto(
                        repositoryName,
                        row.SourceName,
                        string.Empty,
                        0,
                        1,
                        false,
                        false,
                        [new LabelRemapItemErrorDto(0, ex.Message)]));
                }

                continue;
            }

            var outgoingLabelNames = new HashSet<string>(
                CollectOutgoingLabelNames(preview),
                StringComparer.OrdinalIgnoreCase);

            if (outgoingLabelNames.Contains(row.DestinationName!))
            {
                results.Add(new LabelRemapResultDto(
                    repositoryName,
                    row.SourceName,
                    row.DestinationName!,
                    0,
                    1,
                    false,
                    false,
                    [new LabelRemapItemErrorDto(0, $"Cannot remap onto '{row.DestinationName}' because that label is scheduled for deletion.")]));
                continue;
            }

            try
            {
                var remapResult = await LabelManagerService.RemapLabelAsync(
                    owner,
                    repositoryName,
                    row.SourceName,
                    row.DestinationName!,
                    progress);
                results.Add(remapResult);
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new LabelRemapResultDto(
                    repositoryName,
                    row.SourceName,
                    row.DestinationName!,
                    0,
                    1,
                    false,
                    false,
                    [new LabelRemapItemErrorDto(0, ex.Message)]));
            }
        }

        return results;
    }

    private static void ReportTaxonomyProgress(IProgress<string>? progress, string message)
        => progress?.Report(message);

    /// <summary>Represents one remap row in the Recommended taxonomy preview.</summary>
    /// <param name="SourceName">The extra label to remap or delete.</param>
    /// <param name="DestinationName">The destination label, or <see langword="null" /> when skipped.</param>
    /// <param name="DeleteWithoutRemap">Whether the source should be deleted without retagging.</param>
    private sealed record LabelRemapRowState(string SourceName, string? DestinationName, bool DeleteWithoutRemap);
}
