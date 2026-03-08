using Microsoft.AspNetCore.Components;
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
    private IReadOnlyList<string> selectedRepositoryFullNames = [];
    private IReadOnlyList<LabelRow> rows = [];
    private IReadOnlyList<LabelRow> filteredRows = [];
    private bool isLoadingRepositories = true;
    private bool isLoadingLabels;
    private bool hasLoadedLabels;
    private string? errorMessage;
    private bool hasInitialised;

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
        errorMessage = null;
        rows = [];
        filteredRows = [];
        hasLoadedLabels = false;

        try
        {
            availableRepositories = await RepositoryService.GetRepositoriesAsync();
            selectedRepositoryFullNames = [];
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load labels.");
            errorMessage = "An unexpected error occurred while loading labels.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private void OnRepositorySelectionChanged(string repositoryFullName, object? value)
    {
        var isSelected = value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false,
        };

        var selected = selectedRepositoryFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (isSelected)
        {
            selected.Add(repositoryFullName);
        }
        else
        {
            selected.Remove(repositoryFullName);
        }

        selectedRepositoryFullNames = selected.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private async Task LoadLabelsForSelectionAsync()
    {
        errorMessage = null;
        hasLoadedLabels = true;

        if (selectedRepositoryFullNames.Count == 0)
        {
            rows = [];
            ApplyFilter();
            return;
        }

        isLoadingLabels = true;

        try
        {
            var selectedRepositories = availableRepositories
                .Where(repository => selectedRepositoryFullNames.Contains(repository.FullName, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            var repositoryGroups = selectedRepositories
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

    private bool ShowInitialState => !ShowLoadingState && string.IsNullOrWhiteSpace(errorMessage) && !hasLoadedLabels;

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
