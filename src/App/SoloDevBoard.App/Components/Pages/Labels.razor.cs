using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Components.Dialogs;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays a consolidated view of labels across selected repositories.</summary>
public partial class Labels : ComponentBase
{
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

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private IReadOnlyList<RepositoryDto> selectedRepositories = [];
    private IReadOnlyList<LabelRow> rows = [];
    private IReadOnlyList<LabelRow> filteredRows = [];
    private bool isLoadingRepositories = true;
    private bool isLoadingLabels;
    private bool hasLoadedLabels;
    private bool hasRepositoryLoadFailure;
    private string? errorMessage;
    private IReadOnlyList<RecommendedLabelStrategyDto> recommendedStrategies = [];
    private string selectedStrategyId = string.Empty;
    private IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto> recommendedPreview = [];
    private IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> recommendedApplyResults = [];
    private bool showRecommendedPreview;
    private bool isPreviewingRecommendedTaxonomy;
    private bool isApplyingRecommendedTaxonomy;
    private string? taxonomyOperationMessage;
    private Severity taxonomyOperationSeverity = Severity.Info;
    private string syncSourceRepositoryFullName = string.Empty;
    private HashSet<string> syncTargetRepositoryFullNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<LabelSyncRepositoryPreviewDto> syncPreviewResults = [];
    private IReadOnlyList<LabelSyncRepositoryResultDto> syncApplyResults = [];
    private bool showSyncPreview;
    private bool isPreviewingSync;
    private bool isApplyingSync;
    private string? syncOperationMessage;
    private Severity syncOperationSeverity = Severity.Info;

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
            taxonomyOperationSeverity = Severity.Error;
            taxonomyOperationMessage = "Unable to load recommended taxonomy strategies.";
        }
    }

    private async Task ReloadRepositoriesAsync()
    {
        await LoadRepositoriesAsync();
    }

    private Task OnStrategySelectedAsync(string strategyId)
    {
        selectedStrategyId = strategyId;
        recommendedPreview = [];
        showRecommendedPreview = false;
        recommendedApplyResults = [];
        taxonomyOperationMessage = null;
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
            taxonomyOperationMessage = null;
            ResetSyncWorkflow();
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

        recommendedPreview = [];
        showRecommendedPreview = false;
        recommendedApplyResults = [];
        taxonomyOperationMessage = null;
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
        syncOperationMessage = null;
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
        syncOperationMessage = null;
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
            Snackbar.Add("Select at least one repository before previewing taxonomy changes.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedStrategyId))
        {
            Snackbar.Add("Select a recommended strategy before previewing taxonomy changes.", Severity.Warning);
            return;
        }

        isPreviewingRecommendedTaxonomy = true;
        try
        {
            taxonomyOperationMessage = null;
            recommendedApplyResults = [];
            recommendedPreview = await LabelManagerService.PreviewRecommendedTaxonomyAsync(selectedStrategyId, selectedFullNames);
            showRecommendedPreview = true;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while previewing strategy {StrategyId}.", selectedStrategyId);
            taxonomyOperationSeverity = Severity.Error;
            taxonomyOperationMessage = $"GitHub API request failed while previewing taxonomy changes. {ex.Message}";
            showRecommendedPreview = false;
            recommendedPreview = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview strategy {StrategyId}.", selectedStrategyId);
            taxonomyOperationSeverity = Severity.Error;
            taxonomyOperationMessage = "An unexpected error occurred while previewing taxonomy changes.";
            showRecommendedPreview = false;
            recommendedPreview = [];
        }
        finally
        {
            isPreviewingRecommendedTaxonomy = false;
        }
    }

    private void CancelRecommendedPreview()
    {
        showRecommendedPreview = false;
        recommendedPreview = [];
        taxonomyOperationMessage = "Taxonomy apply was cancelled.";
        taxonomyOperationSeverity = Severity.Info;
    }

    private async Task ApplyRecommendedTaxonomyAsync()
    {
        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            Snackbar.Add("Select at least one repository before applying taxonomy changes.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedStrategyId))
        {
            Snackbar.Add("Select a recommended strategy before applying taxonomy changes.", Severity.Warning);
            return;
        }

        isApplyingRecommendedTaxonomy = true;
        try
        {
            recommendedApplyResults = await LabelManagerService.ApplyRecommendedTaxonomyAsync(selectedStrategyId, selectedFullNames);
            showRecommendedPreview = false;
            recommendedPreview = [];

            var failedCount = recommendedApplyResults.Count(result => result.HasError);
            var createdCount = recommendedApplyResults.Sum(result => result.CreatedCount);
            var updatedCount = recommendedApplyResults.Sum(result => result.UpdatedCount);
            var skippedCount = recommendedApplyResults.Sum(result => result.SkippedCount);

            if (failedCount == 0)
            {
                taxonomyOperationSeverity = Severity.Success;
                taxonomyOperationMessage = $"Applied taxonomy successfully. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.";
            }
            else
            {
                taxonomyOperationSeverity = Severity.Warning;
                taxonomyOperationMessage = $"Applied taxonomy with {failedCount} repository errors. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.";
            }

            await LoadLabelsForSelectionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply strategy {StrategyId}.", selectedStrategyId);
            taxonomyOperationSeverity = Severity.Error;
            taxonomyOperationMessage = "An unexpected error occurred while applying taxonomy changes.";
        }
        finally
        {
            isApplyingRecommendedTaxonomy = false;
        }
    }

    private async Task LoadLabelsForSelectionAsync()
    {
        hasRepositoryLoadFailure = false;
        errorMessage = null;
        hasLoadedLabels = true;

        var selectedRepositoryList = selectedRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository.FullName))
            .DistinctBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var selectedRepositoryFullNames = selectedRepositoryList
            .Select(repository => repository.FullName)
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
            var repositoryGroups = selectedRepositoryList
                .Select(repository => (Repository: repository, Owner: ResolveOwner(repository.FullName)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Owner))
                .GroupBy(item => item.Owner, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var consolidatedLabels = new List<LabelDto>();

            foreach (var ownerGroup in repositoryGroups)
            {
                var owner = ownerGroup.Key;
                var repositoriesForOwner = ownerGroup
                    .Select(item => item.Repository.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var labels = await LabelManagerService
                    .GetLabelsForRepositoriesAsync(owner, repositoriesForOwner);

                consolidatedLabels.AddRange(labels.Select(label => label with { RepositoryName = $"{owner}/{label.RepositoryName}" }));
            }

            BuildRows(consolidatedLabels, selectedRepositoryFullNames);
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
            Snackbar.Add("Select a source repository before previewing label synchronisation.", Severity.Warning);
            return;
        }

        var targets = syncTargetRepositoryFullNames
            .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (targets.Length == 0)
        {
            Snackbar.Add("Select at least one target repository before previewing label synchronisation.", Severity.Warning);
            return;
        }

        isPreviewingSync = true;
        try
        {
            syncOperationMessage = null;
            syncApplyResults = [];
            syncPreviewResults = await LabelManagerService.PreviewLabelSynchronisationAsync(syncSourceRepositoryFullName, targets);
            showSyncPreview = true;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while previewing synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            syncOperationSeverity = Severity.Error;
            syncOperationMessage = $"GitHub API request failed while previewing synchronisation. {ex.Message}";
            showSyncPreview = false;
            syncPreviewResults = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            syncOperationSeverity = Severity.Error;
            syncOperationMessage = "An unexpected error occurred while previewing synchronisation.";
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
        syncOperationMessage = "Label synchronisation preview was cancelled. No changes were applied.";
        syncOperationSeverity = Severity.Info;
    }

    private async Task ApplyLabelSynchronisationAsync()
    {
        if (string.IsNullOrWhiteSpace(syncSourceRepositoryFullName))
        {
            Snackbar.Add("Select a source repository before applying label synchronisation.", Severity.Warning);
            return;
        }

        var targets = syncTargetRepositoryFullNames
            .Where(target => !target.Equals(syncSourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (targets.Length == 0)
        {
            Snackbar.Add("Select at least one target repository before applying label synchronisation.", Severity.Warning);
            return;
        }

        isApplyingSync = true;
        try
        {
            syncApplyResults = await LabelManagerService.ApplyLabelSynchronisationAsync(syncSourceRepositoryFullName, targets);
            showSyncPreview = false;
            syncPreviewResults = [];

            var failedCount = syncApplyResults.Count(result => result.HasError);
            var createdCount = syncApplyResults.Sum(result => result.CreatedCount);
            var updatedCount = syncApplyResults.Sum(result => result.UpdatedCount);
            var deletedCount = syncApplyResults.Sum(result => result.DeletedCount);
            var skippedCount = syncApplyResults.Sum(result => result.SkippedCount);

            if (failedCount == 0)
            {
                syncOperationSeverity = Severity.Success;
                syncOperationMessage = $"Synchronisation completed. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.";
            }
            else
            {
                syncOperationSeverity = Severity.Warning;
                syncOperationMessage = $"Synchronisation completed with {failedCount} repository failures. Created {createdCount}, updated {updatedCount}, deleted {deletedCount}, skipped {skippedCount}.";
            }

            await LoadLabelsForSelectionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply synchronisation from {SourceRepository}.", syncSourceRepositoryFullName);
            syncOperationSeverity = Severity.Error;
            syncOperationMessage = "An unexpected error occurred while applying synchronisation.";
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
            Snackbar.Add("Select at least one repository before creating a label.", Severity.Warning);
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

    private async Task ShowEditDialogAsync(LabelRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            Snackbar.Add("Select at least one repository before editing a label.", Severity.Warning);
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
            row.Description == "No description" ? string.Empty : row.Description,
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

    private async Task ShowDeleteDialogAsync(LabelRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            Snackbar.Add("Select at least one repository before deleting a label.", Severity.Warning);
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
            Snackbar.Add("No changes were saved. Please use the form action button in the dialog.", Severity.Warning);
            return null;
        }

        return result;
    }

    private async Task ExecuteCreateAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = GroupRepositoriesByOwner(operation.SelectedRepositories);
        var createRequest = new LabelDto(operation.LabelName, operation.Colour, operation.Description, string.Empty);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                var created = await LabelManagerService.CreateLabelAsync(ownerGroup.Key, ownerGroup.Value, createRequest);
                changedRepositoryCount += created.Count;
            }

            Snackbar.Add($"Created '{operation.LabelName}' in {changedRepositoryCount} repositories.", Severity.Success);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while creating label {LabelName}.", operation.LabelName);
            Snackbar.Add($"GitHub API request failed while creating '{operation.LabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create label {LabelName}.", operation.LabelName);
            Snackbar.Add($"An unexpected error occurred while creating '{operation.LabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private async Task ExecuteUpdateAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = GroupRepositoriesByOwner(operation.SelectedRepositories);
        var updateRequest = new LabelDto(operation.LabelName, operation.Colour, operation.Description, string.Empty);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                var updated = await LabelManagerService.UpdateLabelAsync(ownerGroup.Key, ownerGroup.Value, operation.OriginalLabelName, updateRequest);
                changedRepositoryCount += updated.Count;
            }

            Snackbar.Add($"Updated '{operation.OriginalLabelName}' across {changedRepositoryCount} repositories.", Severity.Success);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while updating label {LabelName}.", operation.OriginalLabelName);
            Snackbar.Add($"GitHub API request failed while updating '{operation.OriginalLabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update label {LabelName}.", operation.OriginalLabelName);
            Snackbar.Add($"An unexpected error occurred while updating '{operation.OriginalLabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private async Task ExecuteDeleteAsync(LabelOperationDialogResult operation)
    {
        var repositoriesByOwner = GroupRepositoriesByOwner(operation.SelectedRepositories);

        try
        {
            var changedRepositoryCount = 0;

            foreach (var ownerGroup in repositoriesByOwner)
            {
                await LabelManagerService.DeleteLabelAsync(ownerGroup.Key, ownerGroup.Value, operation.OriginalLabelName);
                changedRepositoryCount += ownerGroup.Value.Count;
            }

            Snackbar.Add($"Deleted '{operation.OriginalLabelName}' from {changedRepositoryCount} repositories.", Severity.Success);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while deleting label {LabelName}.", operation.OriginalLabelName);
            Snackbar.Add($"GitHub API request failed while deleting '{operation.OriginalLabelName}'. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete label {LabelName}.", operation.OriginalLabelName);
            Snackbar.Add($"An unexpected error occurred while deleting '{operation.OriginalLabelName}'.", Severity.Error);
        }
        finally
        {
            await LoadLabelsForSelectionAsync();
        }
    }

    private void BuildRows(IReadOnlyList<LabelDto> labels, IReadOnlyList<string> selectedFullNames)
    {
        var selectedSet = selectedFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        rows = labels
            .GroupBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var repositoriesWithLabel = group
                    .Select(label => label.RepositoryName)
                    .Where(repositoryName => !string.IsNullOrWhiteSpace(repositoryName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repositoryName => repositoryName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var missingRepositories = selectedSet
                    .Except(repositoriesWithLabel, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repositoryName => repositoryName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new LabelRow(
                    first.Name,
                    first.Colour,
                    string.IsNullOrWhiteSpace(first.Description) ? "No description" : first.Description,
                    repositoriesWithLabel,
                    missingRepositories);
            })
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ApplyFilter();
    }

    private static string ResolveOwner(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private static string ResolveRepositoryName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
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

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> GroupRepositoriesByOwner(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        return repositoryFullNames
            .Select(fullName => new
            {
                Owner = ResolveOwner(fullName),
                Repository = ResolveRepositoryName(fullName),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Owner) && !string.IsNullOrWhiteSpace(item.Repository))
            .GroupBy(item => item.Owner, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(item => item.Repository)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
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

    private bool CanPreviewRecommendedTaxonomy => !ShowLoadingState
        && !isPreviewingRecommendedTaxonomy
        && !isApplyingRecommendedTaxonomy
        && selectedRepositories.Count > 0
        && !string.IsNullOrWhiteSpace(selectedStrategyId);

    private bool CanApplyRecommendedTaxonomy => showRecommendedPreview
        && recommendedPreview.Count > 0
        && !isApplyingRecommendedTaxonomy;

    private bool CanPreviewLabelSynchronisation => !ShowLoadingState
        && !isPreviewingSync
        && !isApplyingSync
        && !string.IsNullOrWhiteSpace(syncSourceRepositoryFullName)
        && syncTargetRepositoryFullNames.Count > 0;

    private bool CanApplyLabelSynchronisation => showSyncPreview
        && syncPreviewResults.Count > 0
        && !isApplyingSync;

    private string SelectedStrategyDescription
        => recommendedStrategies.FirstOrDefault(strategy => strategy.Id.Equals(selectedStrategyId, StringComparison.OrdinalIgnoreCase))?.Description
            ?? string.Empty;

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
        syncOperationMessage = null;
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
        syncOperationMessage = null;
        syncOperationSeverity = Severity.Info;
    }

    /// <summary>Represents a consolidated label row for the grid view.</summary>
    /// <param name="Name">The label name.</param>
    /// <param name="Colour">The hexadecimal colour (without #).</param>
    /// <param name="Description">The label description text.</param>
    /// <param name="RepositoriesWithLabel">Repositories that contain the label.</param>
    /// <param name="MissingRepositories">Repositories currently missing the label.</param>
    public sealed record LabelRow(
        string Name,
        string Colour,
        string Description,
        IReadOnlyList<string> RepositoriesWithLabel,
        IReadOnlyList<string> MissingRepositories)
    {
        /// <summary>Gets repository names containing the label as readable text.</summary>
        public string RepositoriesWithLabelText => RepositoriesWithLabel.Count == 0
            ? "None"
            : string.Join(", ", RepositoriesWithLabel);

        /// <summary>Gets repository names missing the label as readable text.</summary>
        public string MissingRepositoriesText => MissingRepositories.Count == 0
            ? "None"
            : string.Join(", ", MissingRepositories);
    }
}
