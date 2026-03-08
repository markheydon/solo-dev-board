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
    private IReadOnlyList<string> selectedRepositoryNames = [];
    private IReadOnlyList<LabelRow> rows = [];
    private IReadOnlyList<LabelRow> filteredRows = [];
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            availableRepositories = await RepositoryService.GetRepositoriesAsync();
            selectedRepositoryNames = availableRepositories.Select(repository => repository.Name).ToArray();

            await RefreshRowsAsync();
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"GitHub API request failed. {ex.Message}";
            rows = [];
            filteredRows = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load labels.");
            errorMessage = "An unexpected error occurred while loading labels.";
            rows = [];
            filteredRows = [];
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnRepositorySelectionChanged(string repositoryName, object? value)
    {
        var isSelected = value is bool b && b;
        var selected = selectedRepositoryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (isSelected)
        {
            selected.Add(repositoryName);
        }
        else
        {
            selected.Remove(repositoryName);
        }

        selectedRepositoryNames = selected.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        await RefreshRowsAsync();
    }

    private async Task RefreshRowsAsync()
    {
        if (selectedRepositoryNames.Count == 0)
        {
            rows = [];
            ApplyFilter();
            return;
        }

        var owner = ResolveOwner();
        if (string.IsNullOrWhiteSpace(owner))
        {
            rows = [];
            ApplyFilter();
            return;
        }

        var labels = await LabelManagerService.GetLabelsForRepositoriesAsync(owner, selectedRepositoryNames);

        var selectedSet = selectedRepositoryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

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

    private string ResolveOwner()
    {
        var firstFullName = availableRepositories
            .Select(repository => repository.FullName)
            .FirstOrDefault(fullName => !string.IsNullOrWhiteSpace(fullName));

        if (string.IsNullOrWhiteSpace(firstFullName))
        {
            return string.Empty;
        }

        var parts = firstFullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
