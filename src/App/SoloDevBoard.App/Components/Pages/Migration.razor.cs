using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Provides the One-Click Migration workflow for labels and milestones.</summary>
public partial class Migration : ComponentBase
{
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

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private IReadOnlyList<RepositoryDto> selectedRepositories = [];
    private string sourceRepositoryFullName = string.Empty;
    private HashSet<string> targetRepositoryFullNames = new(StringComparer.OrdinalIgnoreCase);
    private bool migrateLabels = true;
    private bool migrateMilestones = true;
    private MigrationConflictStrategy conflictStrategy = MigrationConflictStrategy.Skip;
    private MigrationPreviewDto previewResult = new(MigrationConflictStrategy.Skip, [], []);
    private MigrationResultDto applyResult = new(MigrationConflictStrategy.Skip, [], []);
    private bool isLoadingRepositories = true;
    private bool isPreviewing;
    private bool isApplying;
    private bool showPreview;
    private string? operationMessage;
    private Severity operationSeverity = Severity.Info;

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

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        operationMessage = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedRepositories = [];
            ResetWorkflow();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading migration repositories.");
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load migration repositories.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while loading repositories.";
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

        EnsureSelectionState();
        return Task.CompletedTask;
    }

    private Task OnSourceRepositoryChangedAsync(string value)
    {
        sourceRepositoryFullName = value ?? string.Empty;
        _ = targetRepositoryFullNames.Remove(sourceRepositoryFullName);
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnTargetRepositoryChangedAsync(string repositoryFullName, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(repositoryFullName) || repositoryFullName.Equals(sourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (isSelected)
        {
            _ = targetRepositoryFullNames.Add(repositoryFullName);
        }
        else
        {
            _ = targetRepositoryFullNames.Remove(repositoryFullName);
        }

        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnConflictStrategyChangedAsync(MigrationConflictStrategy value)
    {
        conflictStrategy = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnMigrateLabelsChangedAsync(bool value)
    {
        migrateLabels = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private Task OnMigrateMilestonesChangedAsync(bool value)
    {
        migrateMilestones = value;
        ResetPreviewAndResults();
        return Task.CompletedTask;
    }

    private async Task PreviewMigrationAsync()
    {
        if (isPreviewing)
        {
            return;
        }

        if (!CanPreview)
        {
            Snackbar.Add("Select one source repository, at least one target repository, and at least one migration item before previewing migration.", Severity.Warning);
            return;
        }

        isPreviewing = true;
        operationMessage = null;

        try
        {
            previewResult = await MigrationService.PreviewMigrationAsync(
                sourceRepositoryFullName,
                targetRepositoryFullNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
                BuildScope(),
                conflictStrategy);

            showPreview = true;
            applyResult = new MigrationResultDto(conflictStrategy, [], []);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while previewing migration from {SourceRepository}.", sourceRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while previewing migration. {ex.Message}";
            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], []);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview migration from {SourceRepository}.", sourceRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while previewing migration.";
            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], []);
        }
        finally
        {
            isPreviewing = false;
        }
    }

    private void CancelPreview()
    {
        showPreview = false;
        previewResult = new MigrationPreviewDto(conflictStrategy, [], []);
        operationSeverity = Severity.Info;
        operationMessage = "Migration preview was cancelled. No changes were applied.";
    }

    private async Task ApplyMigrationAsync()
    {
        if (!CanApply)
        {
            Snackbar.Add("Preview migration before applying changes.", Severity.Warning);
            return;
        }

        isApplying = true;
        operationMessage = null;

        try
        {
            applyResult = await MigrationService.ApplyMigrationAsync(
                sourceRepositoryFullName,
                targetRepositoryFullNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
                BuildScope(),
                conflictStrategy);

            showPreview = false;
            previewResult = new MigrationPreviewDto(conflictStrategy, [], []);

            var labelFailures = applyResult.LabelResults.Count(result => result.HasError);
            var milestoneFailures = applyResult.MilestoneResults.Count(result => result.HasError);
            var totalFailures = labelFailures + milestoneFailures;

            if (totalFailures == 0)
            {
                operationSeverity = Severity.Success;
                operationMessage = "Migration completed successfully for labels and milestones.";
            }
            else
            {
                operationSeverity = Severity.Warning;
                operationMessage = $"Migration completed with {totalFailures} repository operation errors.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply migration from {SourceRepository}.", sourceRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while applying migration.";
        }
        finally
        {
            isApplying = false;
        }
    }

    private bool IsBusy => isLoadingRepositories || isPreviewing || isApplying;

    private bool CanPreview => !IsBusy
        && selectedRepositories.Count >= 2
        && !string.IsNullOrWhiteSpace(sourceRepositoryFullName)
        && targetRepositoryFullNames.Count > 0
        && (migrateLabels || migrateMilestones);

    private bool CanApply => showPreview
        && !IsBusy
        && HasActionablePreviewChanges;

    private bool HasActionablePreviewChanges
    {
        get
        {
            var labelActions = previewResult.LabelPreviews.Sum(preview => preview.ToCreate.Count + preview.ToUpdate.Count + preview.ToDelete.Count);
            var milestoneActions = previewResult.MilestonePreviews.Sum(preview => preview.ToCreate.Count + preview.ToUpdate.Count + preview.ToDelete.Count);
            return labelActions + milestoneActions > 0;
        }
    }

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

    private IReadOnlyList<string> orderedPreviewRepositories
        => previewResult.LabelPreviews
            .Select(preview => preview.RepositoryFullName)
            .Union(previewResult.MilestonePreviews.Select(preview => preview.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> orderedApplyRepositories
        => applyResult.LabelResults
            .Select(result => result.RepositoryFullName)
            .Union(applyResult.MilestoneResults.Select(result => result.RepositoryFullName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private LabelSyncRepositoryPreviewDto GetLabelPreview(string repositoryFullName)
        => previewResult.LabelPreviews.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new LabelSyncRepositoryPreviewDto(repositoryFullName, [], [], [], []);

    private MilestoneSyncRepositoryPreviewDto GetMilestonePreview(string repositoryFullName)
        => previewResult.MilestonePreviews.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new MilestoneSyncRepositoryPreviewDto(repositoryFullName, [], [], [], []);

    private LabelSyncRepositoryResultDto GetLabelResult(string repositoryFullName)
        => applyResult.LabelResults.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new LabelSyncRepositoryResultDto(repositoryFullName, 0, 0, 0, 0, null);

    private MilestoneSyncRepositoryResultDto GetMilestoneResult(string repositoryFullName)
        => applyResult.MilestoneResults.FirstOrDefault(item => item.RepositoryFullName.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            ?? new MilestoneSyncRepositoryResultDto(repositoryFullName, 0, 0, 0, 0, null);

    private static bool HasActionableChanges(LabelSyncRepositoryPreviewDto labelPreview, MilestoneSyncRepositoryPreviewDto milestonePreview)
        => (labelPreview.ToCreate.Count + labelPreview.ToUpdate.Count + labelPreview.ToDelete.Count
            + milestonePreview.ToCreate.Count + milestonePreview.ToUpdate.Count + milestonePreview.ToDelete.Count) > 0;

    private void ResetWorkflow()
    {
        sourceRepositoryFullName = string.Empty;
        targetRepositoryFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        migrateLabels = true;
        migrateMilestones = true;
        conflictStrategy = MigrationConflictStrategy.Skip;
        ResetPreviewAndResults();
    }

    private void ResetPreviewAndResults()
    {
        showPreview = false;
        previewResult = new MigrationPreviewDto(conflictStrategy, [], []);
        applyResult = new MigrationResultDto(conflictStrategy, [], []);
        operationMessage = null;
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

        ResetPreviewAndResults();
    }

    private MigrationScopeDto BuildScope() => new(migrateLabels, migrateMilestones);

    private sealed record ConflictOption(MigrationConflictStrategy Value, string Label, string Description);
}
