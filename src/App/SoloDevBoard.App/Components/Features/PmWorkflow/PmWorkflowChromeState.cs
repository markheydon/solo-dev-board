using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Shared PM Workflow chrome state cascaded to tab pages.</summary>
public sealed class PmWorkflowChromeState
{
    /// <summary>Gets or sets the effective PM settings.</summary>
    public PmSettingsDto Settings { get; set; } = PmSettingsDefaults.Create();

    /// <summary>Gets or sets the active repositories available for inclusion.</summary>
    public IReadOnlyList<RepositoryDto> ActiveRepositories { get; set; } = [];

    /// <summary>Gets or sets the discovered planning board options.</summary>
    public IReadOnlyList<PmPlanningBoardOptionDto> PlanningBoardOptions { get; set; } = [];

    /// <summary>Gets or sets the inaccessible project boards warning, if any.</summary>
    public string? InaccessibleProjectBoardsWarning { get; set; }

    /// <summary>Gets or sets the UTC timestamp when data was last refreshed.</summary>
    public DateTimeOffset? LastRefreshedAtUtc { get; set; }

    /// <summary>Gets or sets a value indicating whether chrome data is loading.</summary>
    public bool IsLoading { get; set; }

    /// <summary>Gets or sets the chrome load error message, if any.</summary>
    public string? LoadErrorMessage { get; set; }

    /// <summary>Gets the number of active repositories included after exclusions.</summary>
    public int IncludedRepositoryCount =>
        ActiveRepositories.Count(repository => !IsRepositoryExcluded(repository.FullName));

    /// <summary>Gets the title of the selected planning board, when known.</summary>
    public string? SelectedPlanningBoardTitle =>
        PlanningBoardOptions.FirstOrDefault(option =>
            option.Id.Equals(Settings.PlanningBoardNodeId, StringComparison.Ordinal))?.Title;

    /// <summary>Gets a value indicating whether a planning board is selected.</summary>
    public bool HasPlanningBoardSelected => !string.IsNullOrWhiteSpace(Settings.PlanningBoardNodeId);

    /// <summary>Gets or sets the callback used to persist updated settings.</summary>
    public Func<PmSettingsDto, Task> SaveSettingsAsync { get; set; } = _ => Task.CompletedTask;

    /// <summary>Gets or sets the callback used to reload chrome data.</summary>
    public Func<Task> RefreshAsync { get; set; } = () => Task.CompletedTask;

    /// <summary>Returns whether the repository is excluded from PM queries.</summary>
    /// <param name="repositoryFullName">The repository full name.</param>
    /// <returns><see langword="true" /> when excluded; otherwise <see langword="false" />.</returns>
    public bool IsRepositoryExcluded(string repositoryFullName) =>
        Settings.ExcludedRepositories.Contains(repositoryFullName, StringComparer.OrdinalIgnoreCase);
}
