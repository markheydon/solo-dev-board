using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SoloDevBoard.App.Components.Dialogs;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

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

    /// <summary>Gets or sets the Fluent dialog service for label operations.</summary>
    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    /// <summary>Gets or sets the Fluent toast service for user feedback.</summary>
    [Inject]
    public IToastService ToastService { get; set; } = default!;

    private IReadOnlyList<Repository> availableRepositories = [];
    private IEnumerable<Repository> visibleRepositoryOptions = [];
    private IEnumerable<Repository> selectedRepositories = [];
    private IReadOnlyList<LabelRow> rows = [];
    private IReadOnlyList<LabelRow> filteredRows = [];
    private bool isLoadingRepositories = true;
    private bool isLoadingLabels;
    private bool hasLoadedLabels;
    private bool hasRepositoryLoadFailure;
    private string? errorMessage;
    private bool hasInitialised;
    private string repositorySearchText = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || hasInitialised)
        {
            return;
        }

        hasInitialised = true;
        await LoadRepositoriesAsync();
        StateHasChanged();
    }

    private async Task ReloadRepositoriesAsync()
    {
        await LoadRepositoriesAsync();
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

            visibleRepositoryOptions = availableRepositories;
            selectedRepositories = [];
            repositorySearchText = string.Empty;
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

    private void OnRepositoryOptionsSearch(OptionsSearchEventArgs<Repository> args)
    {
        if (string.IsNullOrWhiteSpace(args.Text))
        {
            visibleRepositoryOptions = availableRepositories;
            args.Items = visibleRepositoryOptions;
            return;
        }

        var filter = args.Text.Trim();
        visibleRepositoryOptions = availableRepositories
            .Where(repository => repository.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        args.Items = visibleRepositoryOptions;
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

    private async Task ShowCreateDialogAsync()
    {
        if (!TryGetSelectedRepositoryFullNames(out var selectedFullNames))
        {
            ToastService.ShowWarning("Select at least one repository before creating a label.");
            return;
        }

        var request = new LabelOperationDialogRequest(
            LabelOperationMode.Create,
            string.Empty,
            string.Empty,
            "#ededed",
            string.Empty,
            selectedFullNames,
            selectedFullNames);

        var result = await ShowLabelOperationDialogAsync("New label", "Create", request);
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
            ToastService.ShowWarning("Select at least one repository before editing a label.");
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
            defaultSelection);

        var result = await ShowLabelOperationDialogAsync("Edit label", "Save", request);
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
            ToastService.ShowWarning("Select at least one repository before deleting a label.");
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
            defaultSelection);

        var result = await ShowLabelOperationDialogAsync("Delete label", "Delete", request);
        if (result is null)
        {
            return;
        }

        await ExecuteDeleteAsync(result);
    }

    private async Task<LabelOperationDialogResult?> ShowLabelOperationDialogAsync(string title, string primaryAction, LabelOperationDialogRequest request)
    {
        var dialog = await DialogService.ShowDialogAsync<LabelOperationDialog, LabelOperationDialogRequest>(
            request,
            new DialogParameters
            {
                Title = title,
                PrimaryAction = primaryAction,
                SecondaryAction = "Cancel",
                Width = "36rem",
                PreventDismissOnOverlayClick = true,
            });

        var dialogResult = await dialog.Result;
        if (dialogResult.Cancelled)
        {
            return null;
        }

        return dialogResult.Data as LabelOperationDialogResult;
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

            ToastService.ShowSuccess($"Created '{operation.LabelName}' in {changedRepositoryCount} repositories.");
            await LoadLabelsForSelectionAsync();
        }
        catch (HttpRequestException ex)
        {
            ToastService.ShowError($"GitHub API request failed while creating '{operation.LabelName}'. {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create label {LabelName}.", operation.LabelName);
            ToastService.ShowError($"An unexpected error occurred while creating '{operation.LabelName}'.");
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

            ToastService.ShowSuccess($"Updated '{operation.OriginalLabelName}' across {changedRepositoryCount} repositories.");
            await LoadLabelsForSelectionAsync();
        }
        catch (HttpRequestException ex)
        {
            ToastService.ShowError($"GitHub API request failed while updating '{operation.OriginalLabelName}'. {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update label {LabelName}.", operation.OriginalLabelName);
            ToastService.ShowError($"An unexpected error occurred while updating '{operation.OriginalLabelName}'.");
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

            ToastService.ShowSuccess($"Deleted '{operation.OriginalLabelName}' from {changedRepositoryCount} repositories.");
            await LoadLabelsForSelectionAsync();
        }
        catch (HttpRequestException ex)
        {
            ToastService.ShowError($"GitHub API request failed while deleting '{operation.OriginalLabelName}'. {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete label {LabelName}.", operation.OriginalLabelName);
            ToastService.ShowError($"An unexpected error occurred while deleting '{operation.OriginalLabelName}'.");
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

    private bool ShowLabelFilter => hasLoadedLabels && rows.Count > 0 && !ShowLoadingState && string.IsNullOrWhiteSpace(errorMessage);

    private string ErrorTitle => hasRepositoryLoadFailure
        ? "Unable to load repositories"
        : "Unable to load labels";

    private string RepositorySearchText
    {
        get => repositorySearchText;
        set
        {
            repositorySearchText = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                visibleRepositoryOptions = availableRepositories;
                return;
            }

            var filter = value.Trim();
            visibleRepositoryOptions = availableRepositories
                .Where(repository => repository.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    private string RepositorySelectorSummary
        => $"Showing {availableRepositories.Count} active repositories. Archived repositories are hidden by default.";

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
