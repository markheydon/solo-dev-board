using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Shared chrome for Cross-Repo PM Workflow tab pages.</summary>
public partial class PmWorkflowShell : ComponentBase
{
    /// <summary>Gets or sets the active tab route segment.</summary>
    [Parameter]
    public string ActiveTab { get; set; } = string.Empty;

    /// <summary>Gets or sets the tab page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the PM settings service.</summary>
    [Inject]
    public IPmSettingsService PmSettingsService { get; set; } = default!;

    /// <summary>Gets or sets the repository service.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the planning board discovery service.</summary>
    [Inject]
    public IPmProjectBoardDiscoveryService ProjectBoardDiscoveryService { get; set; } = default!;

    /// <summary>Gets or sets the logger.</summary>
    [Inject]
    public ILogger<PmWorkflowShell> Logger { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    private readonly PmWorkflowChromeState chromeState = new();
    private string selectedPlanningBoardId = string.Empty;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        chromeState.SaveSettingsAsync = SaveSettingsAsync;
        chromeState.RefreshAsync = RefreshAsync;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        chromeState.IsLoading = true;
        chromeState.LoadErrorMessage = null;

        try
        {
            chromeState.Settings = await PmSettingsService.GetSettingsAsync();
            selectedPlanningBoardId = chromeState.Settings.PlanningBoardNodeId ?? string.Empty;

            chromeState.ActiveRepositories = await RepositoryService.GetActiveRepositoriesAsync();
            var discovery = await ProjectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
                chromeState.ActiveRepositories);
            chromeState.PlanningBoardOptions = discovery.Options;
            chromeState.InaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);

            if (!chromeState.PlanningBoardOptions.Any(option =>
                    option.Id.Equals(selectedPlanningBoardId, StringComparison.Ordinal)))
            {
                selectedPlanningBoardId = chromeState.PlanningBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
                if (!string.Equals(chromeState.Settings.PlanningBoardNodeId, selectedPlanningBoardId, StringComparison.Ordinal))
                {
                    await SaveSettingsAsync(chromeState.Settings with
                    {
                        PlanningBoardNodeId = string.IsNullOrWhiteSpace(selectedPlanningBoardId) ? null : selectedPlanningBoardId,
                    });
                }
            }

            chromeState.LastRefreshedAtUtc = DateTimeOffset.UtcNow;
            chromeState.MarkDataChanged();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load PM Workflow chrome data.");
            chromeState.LoadErrorMessage = "Unable to load PM Workflow settings. Check your GitHub connection and try again.";
        }
        finally
        {
            chromeState.IsLoading = false;
        }
    }

    private async Task OnPlanningBoardChangedAsync(string? boardId)
    {
        selectedPlanningBoardId = boardId ?? string.Empty;
        await SaveSettingsAsync(chromeState.Settings with
        {
            PlanningBoardNodeId = string.IsNullOrWhiteSpace(selectedPlanningBoardId) ? null : selectedPlanningBoardId,
        });

        var boardTitle = chromeState.SelectedPlanningBoardTitle ?? "Planning board";
        Snackbar.Add($"{boardTitle} selected.", Severity.Success);
    }

    private async Task SaveSettingsAsync(PmSettingsDto settings)
    {
        await PmSettingsService.SaveSettingsAsync(settings);
        chromeState.Settings = await PmSettingsService.GetSettingsAsync();
        selectedPlanningBoardId = chromeState.Settings.PlanningBoardNodeId ?? string.Empty;
        chromeState.MarkDataChanged();
        await InvokeAsync(StateHasChanged);
    }

    private static string FormatLastRefreshed(DateTimeOffset? refreshedAtUtc) =>
        refreshedAtUtc?.ToLocalTime().ToString("g") ?? "Not yet refreshed";

    private int ActiveTabIndex => ActiveTab.ToLowerInvariant() switch
    {
        "daily-focus" => 0,
        "backlog" => 1,
        "planning" => 2,
        "repos" => 3,
        _ => 0,
    };

    private Task OnTabIndexChangedAsync(int index)
    {
        var route = index switch
        {
            0 => "/pm-workflow/daily-focus",
            1 => "/pm-workflow/backlog",
            2 => "/pm-workflow/planning",
            _ => "/pm-workflow/repos",
        };

        if (!NavigationManager.Uri.EndsWith(route, StringComparison.OrdinalIgnoreCase))
        {
            NavigationManager.NavigateTo(route);
        }

        return Task.CompletedTask;
    }
}
