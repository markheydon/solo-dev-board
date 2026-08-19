using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Shared layout that hosts PM Workflow chrome across tab routes.</summary>
public partial class PmWorkflowLayout : LayoutComponentBase, IDisposable
{
    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets or sets the PM Workflow chrome coordinator.</summary>
    [Inject]
    public PmWorkflowChromeCoordinator ChromeCoordinator { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized() =>
        NavigationManager.LocationChanged += OnLocationChanged;

    /// <inheritdoc/>
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        CancelIfLeavingPmWorkflow(NavigationManager.Uri);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) =>
        CancelIfLeavingPmWorkflow(args.Location);

    private void CancelIfLeavingPmWorkflow(string uri)
    {
        if (!IsPmWorkflowPath(uri))
        {
            ChromeCoordinator.CancelPendingLoad();
        }
    }

    private string ResolveActiveTab()
    {
        var path = new Uri(NavigationManager.Uri).AbsolutePath.TrimEnd('/');
        return path switch
        {
            "/pm-workflow/daily-focus" => "daily-focus",
            "/pm-workflow/backlog" => "backlog",
            "/pm-workflow/planning" => "planning",
            "/pm-workflow/repos" => "repos",
            _ => "daily-focus",
        };
    }

    /// <summary>Returns whether the URI is a PM Workflow route.</summary>
    /// <param name="uri">The navigation URI.</param>
    /// <returns><see langword="true" /> when the path is under <c>/pm-workflow</c>; otherwise, <see langword="false" />.</returns>
    internal static bool IsPmWorkflowPath(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return uri.Contains("/pm-workflow", StringComparison.OrdinalIgnoreCase);
        }

        return absolute.AbsolutePath.StartsWith("/pm-workflow", StringComparison.OrdinalIgnoreCase);
    }
}
