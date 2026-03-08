using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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
