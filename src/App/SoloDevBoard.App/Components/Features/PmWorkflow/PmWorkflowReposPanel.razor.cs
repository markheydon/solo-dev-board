using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Repo Management panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
public partial class PmWorkflowReposPanel : ComponentBase
{
    [CascadingParameter]
    public PmWorkflowChromeState? ChromeState { get; set; }

    private int capacity = PmSettingsDefaults.Capacity;
    private int stallDays = PmSettingsDefaults.StallDays;
    private int neglectDays = PmSettingsDefaults.NeglectDays;
    private IReadOnlyList<string> includedRepositoryOptions = [];
    private IReadOnlyList<string> excludedRepositories = [];
    private string? repositoryToExclude;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (ChromeState is null)
        {
            return;
        }

        capacity = ChromeState.Settings.Capacity;
        stallDays = ChromeState.Settings.StallDays;
        neglectDays = ChromeState.Settings.NeglectDays;
        excludedRepositories = ChromeState.Settings.ExcludedRepositories;
        includedRepositoryOptions = ChromeState.ActiveRepositories
            .Select(repository => repository.FullName)
            .Where(fullName => !ChromeState.IsRepositoryExcluded(fullName))
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task SaveThresholdsAsync()
    {
        if (ChromeState is null)
        {
            return;
        }

        await ChromeState.SaveSettingsAsync(ChromeState.Settings with
        {
            Capacity = capacity,
            StallDays = stallDays,
            NeglectDays = neglectDays,
        });
    }

    private async Task ExcludeRepositoryAsync(string? repositoryFullName)
    {
        if (ChromeState is null || string.IsNullOrWhiteSpace(repositoryFullName))
        {
            return;
        }

        var exclusions = ChromeState.Settings.ExcludedRepositories
            .Append(repositoryFullName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await ChromeState.SaveSettingsAsync(ChromeState.Settings with { ExcludedRepositories = exclusions });
        repositoryToExclude = null;
        OnParametersSet();
    }

    private async Task IncludeRepositoryAsync(string repositoryFullName)
    {
        if (ChromeState is null)
        {
            return;
        }

        var exclusions = ChromeState.Settings.ExcludedRepositories
            .Where(repository => !repository.Equals(repositoryFullName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        await ChromeState.SaveSettingsAsync(ChromeState.Settings with { ExcludedRepositories = exclusions });
        OnParametersSet();
    }

    private Task<IEnumerable<string>> SearchIncludedRepositoriesAsync(string? value, CancellationToken cancellationToken)
    {
        IEnumerable<string> matches = includedRepositoryOptions;

        if (!string.IsNullOrWhiteSpace(value))
        {
            var filter = value.Trim();
            matches = matches.Where(repository => repository.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(matches);
    }
}
