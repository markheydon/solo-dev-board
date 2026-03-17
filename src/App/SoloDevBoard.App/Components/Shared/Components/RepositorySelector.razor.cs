using Microsoft.AspNetCore.Components;

namespace SoloDevBoard.App.Components.Shared.Components;

/// <summary>Provides a reusable repository selection control with search and multi-select behaviour.</summary>
public partial class RepositorySelector : ComponentBase
{
    private string? repositoryAutocompleteValue;

    /// <summary>Gets or sets the heading text displayed above the selector.</summary>
    [Parameter]
    public string Title { get; set; } = "Repository selector";

    /// <summary>Gets or sets the input label used by the autocomplete search box.</summary>
    [Parameter]
    public string Label { get; set; } = "Repositories";

    /// <summary>Gets or sets the placeholder text used by the autocomplete search box.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "Search repositories and select";

    /// <summary>Gets or sets the text shown when no repositories are available.</summary>
    [Parameter]
    public string EmptyStateText { get; set; } = "No active repositories are available.";

    /// <summary>Gets or sets the repositories available for selection.</summary>
    [Parameter]
    public IReadOnlyList<string> AvailableRepositories { get; set; } = [];

    /// <summary>Gets or sets the currently selected repositories.</summary>
    [Parameter]
    public IReadOnlyList<string> SelectedRepositories { get; set; } = [];

    /// <summary>Gets or sets the callback raised when selected repositories change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> SelectedRepositoriesChanged { get; set; }

    /// <summary>Gets or sets the summary text shown below the selector controls.</summary>
    [Parameter]
    public string SummaryText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether selected repositories are shown as removable chips.</summary>
    [Parameter]
    public bool ShowSelectedChips { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether select-all and clear actions are shown.</summary>
    [Parameter]
    public bool ShowSelectionActions { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether interaction with the selector is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the data-testid value used for the autocomplete element.</summary>
    [Parameter]
    public string AutocompleteTestId { get; set; } = "repository-autocomplete";

    /// <summary>Gets or sets the data-testid value used for the summary text element.</summary>
    [Parameter]
    public string SummaryTestId { get; set; } = "repository-selector-summary";

    private Task<IEnumerable<string>> SearchRepositoriesAsync(string? value, CancellationToken cancellationToken)
    {
        IEnumerable<string> matches = AvailableRepositories;

        if (!string.IsNullOrWhiteSpace(value))
        {
            var filter = value.Trim();
            matches = matches.Where(repository => repository.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var selectedNames = SelectedRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        matches = matches.Where(repository => !selectedNames.Contains(repository));

        return Task.FromResult(matches);
    }

    private async Task OnRepositorySelectedAsync(string? repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            return;
        }

        if (!AvailableRepositories.Contains(repository, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var selected = SelectedRepositories
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _ = selected.Add(repository);

        repositoryAutocompleteValue = null;

        await SelectedRepositoriesChanged.InvokeAsync(
            selected
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private async Task RemoveSelectedRepositoryAsync(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            return;
        }

        var selected = SelectedRepositories
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _ = selected.Remove(repository);

        await SelectedRepositoriesChanged.InvokeAsync(
            selected
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private async Task SelectAllRepositoriesAsync()
    {
        await SelectedRepositoriesChanged.InvokeAsync(
            AvailableRepositories
                .Where(repository => !string.IsNullOrWhiteSpace(repository))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private async Task ClearSelectedRepositoriesAsync()
    {
        await SelectedRepositoriesChanged.InvokeAsync([]);
    }
}
