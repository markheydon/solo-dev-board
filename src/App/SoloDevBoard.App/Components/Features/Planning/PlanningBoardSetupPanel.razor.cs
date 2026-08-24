using Microsoft.AspNetCore.Components;

namespace SoloDevBoard.App.Components.Features.Planning;

/// <summary>Board setup panel rendered inside the board setup Planning tab page.</summary>
public partial class PlanningBoardSetupPanel : ComponentBase
{
    [CascadingParameter]
    public PlanningChromeState? ChromeState { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    private async Task RecheckAsync()
    {
        if (ChromeState is null)
        {
            return;
        }

        await ChromeState.RecheckBoardCompatibilityAsync().ConfigureAwait(false);

        if (ChromeState.BoardCompatibilityReport is { HasIssues: false })
        {
            NavigationManager.NavigateTo("/planning/daily-focus", replace: true);
            return;
        }

        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }
}
