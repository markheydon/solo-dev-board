using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Repo Management panel rendered inside <see cref="PmWorkflowShell"/>.</summary>
public partial class PmWorkflowReposPanel : ComponentBase, IDisposable
{
    [CascadingParameter]
    public PmWorkflowChromeState? ChromeState { get; set; }

    [CascadingParameter(Name = "PmWorkflowDataRevision")]
    public int DataRevision { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public IPmWorkItemCatalogueService WorkItemCatalogueService { get; set; } = default!;

    [Inject]
    public ILogger<PmWorkflowReposPanel> Logger { get; set; } = default!;

    private int capacity = PmSettingsDefaults.Capacity;
    private int stallDays = PmSettingsDefaults.StallDays;
    private int neglectDays = PmSettingsDefaults.NeglectDays;
    private IReadOnlyList<string> includedRepositoryOptions = [];
    private IReadOnlyList<string> excludedRepositories = [];
    private string? repositoryToExclude;
    private string includedRepositoryFilter = string.Empty;
    private IReadOnlyList<PmRepositorySummaryDto> repositorySummaries = [];
    private IReadOnlyList<PmRepositoryCatalogueFailureDto> summaryFailures = [];
    private bool isLoadingSummaries;
    private string? summaryErrorMessage;
    private int loadedSummaryRevision = -1;
    private int summaryLoadGeneration;
    private CancellationTokenSource? summaryLoadCts;

    private int IncludedRepositoryCount => includedRepositoryOptions.Count;

    private IEnumerable<string> FilteredIncludedRepositories
    {
        get
        {
            if (string.IsNullOrWhiteSpace(includedRepositoryFilter))
            {
                return includedRepositoryOptions;
            }

            var filter = includedRepositoryFilter.Trim();
            return includedRepositoryOptions.Where(repository =>
                repository.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet() => SyncChromeFields();

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        SyncChromeFields();

        if (ChromeState is null || ChromeState.IsLoading)
        {
            return;
        }

        if (loadedSummaryRevision == DataRevision)
        {
            return;
        }

        await LoadSummariesAsync();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        CancelSummaryLoad();
        GC.SuppressFinalize(this);
    }

    private void SyncChromeFields()
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

    private Task RetryLoadSummariesAsync()
    {
        loadedSummaryRevision = -1;
        return LoadSummariesAsync();
    }

    private async Task LoadSummariesAsync()
    {
        var revision = DataRevision;
        var loadCts = BeginSummaryLoad();
        var generation = summaryLoadGeneration;
        loadedSummaryRevision = revision;
        isLoadingSummaries = true;
        summaryErrorMessage = null;

        try
        {
            var catalogue = await WorkItemCatalogueService.GetCatalogueAsync(loadCts.Token);
            if (IsStaleSummaryLoad(loadCts, generation))
            {
                return;
            }

            repositorySummaries = catalogue.RepositorySummaries;
            summaryFailures = catalogue.Failures;
        }
        catch (OperationCanceledException) when (loadCts.IsCancellationRequested)
        {
            Logger.LogDebug("PM repository summary load cancelled.");
        }
        catch (Exception exception)
        {
            if (IsStaleSummaryLoad(loadCts, generation))
            {
                return;
            }

            Logger.LogError(exception, "Failed to load PM repository summaries.");
            repositorySummaries = [];
            summaryFailures = [];
            summaryErrorMessage =
                "Unable to load per-repository issue and pull request counts. Check your GitHub connection and try again.";
        }
        finally
        {
            if (!IsStaleSummaryLoad(loadCts, generation))
            {
                isLoadingSummaries = false;
            }
        }
    }

    private CancellationTokenSource BeginSummaryLoad()
    {
        CancelSummaryLoad();
        summaryLoadCts = new CancellationTokenSource();
        summaryLoadGeneration++;
        return summaryLoadCts;
    }

    private void CancelSummaryLoad()
    {
        if (summaryLoadCts is null)
        {
            return;
        }

        summaryLoadCts.Cancel();
        summaryLoadCts.Dispose();
        summaryLoadCts = null;
    }

    private bool IsStaleSummaryLoad(CancellationTokenSource loadCts, int generation)
        => generation != summaryLoadGeneration
            || !ReferenceEquals(summaryLoadCts, loadCts);

    private static string FormatLastActivity(DateTimeOffset lastActivityAt)
    {
        if (lastActivityAt == default)
        {
            return "No recorded activity";
        }

        var days = Math.Max(0, (int)Math.Floor((DateTimeOffset.UtcNow - lastActivityAt).TotalDays));
        return days switch
        {
            0 => "Today",
            1 => "1 day ago",
            _ => $"{days} days ago",
        };
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

        Snackbar.Add("Planning thresholds saved.", Severity.Success);
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
        Snackbar.Add($"'{repositoryFullName.Trim()}' excluded from PM queries.", Severity.Success);
        SyncChromeFields();
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
        Snackbar.Add($"'{repositoryFullName}' included in PM queries again.", Severity.Success);
        SyncChromeFields();
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
