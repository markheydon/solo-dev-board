using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Shared chrome for Planning tab pages.</summary>
public partial class PmWorkflowShell : ComponentBase
{
    /// <summary>Gets or sets the active tab route segment.</summary>
    [Parameter]
    public string ActiveTab { get; set; } = string.Empty;

    /// <summary>Gets or sets the tab page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the Planning chrome coordinator.</summary>
    [Inject]
    public PmWorkflowChromeCoordinator ChromeCoordinator { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    private PmWorkflowChromeState ChromeState => ChromeCoordinator.State;
    private string selectedPlanningBoardId = string.Empty;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        ChromeState.SaveSettingsAsync = SaveSettingsAsync;
        ChromeState.RefreshAsync = () => ChromeCoordinator.RefreshAsync(forceReload: true);

        // Start before the first paint so IsLoading is true on Daily Focus entry.
        await ChromeCoordinator.EnsureLoadedAsync();
        selectedPlanningBoardId = ChromeState.Settings.PlanningBoardNodeId ?? string.Empty;
    }

    private async Task RefreshAsync()
    {
        await ChromeCoordinator.RefreshAsync(forceReload: true).ConfigureAwait(false);
        selectedPlanningBoardId = ChromeState.Settings.PlanningBoardNodeId ?? string.Empty;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private async Task OnPlanningBoardChangedAsync(string? boardId)
    {
        selectedPlanningBoardId = boardId ?? string.Empty;
        await SaveSettingsAsync(ChromeState.Settings with
        {
            PlanningBoardNodeId = string.IsNullOrWhiteSpace(selectedPlanningBoardId) ? null : selectedPlanningBoardId,
        }).ConfigureAwait(false);
    }

    private async Task SaveSettingsAsync(PmSettingsDto settings)
    {
        await ChromeCoordinator.SaveSettingsAsync(settings).ConfigureAwait(false);
        selectedPlanningBoardId = ChromeState.Settings.PlanningBoardNodeId ?? string.Empty;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
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
