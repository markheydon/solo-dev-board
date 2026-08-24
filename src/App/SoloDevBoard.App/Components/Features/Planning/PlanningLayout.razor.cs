using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SoloDevBoard.App.Components.Features.Planning;

/// <summary>Shared layout that hosts Planning chrome across tab routes.</summary>
public partial class PlanningLayout : LayoutComponentBase, IDisposable
{
    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets or sets the Planning chrome coordinator.</summary>
    [Inject]
    public PlanningChromeCoordinator ChromeCoordinator { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized() =>
        NavigationManager.LocationChanged += OnLocationChanged;

    /// <inheritdoc/>
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        CancelIfLeavingPlanning(NavigationManager.Uri);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) =>
        CancelIfLeavingPlanning(args.Location);

    private void CancelIfLeavingPlanning(string uri)
    {
        if (!IsPlanningPath(uri))
        {
            ChromeCoordinator.CancelPendingLoad();
        }
    }

    private string ResolveActiveTab()
    {
        var path = new Uri(NavigationManager.Uri).AbsolutePath.TrimEnd('/');
        return path switch
        {
            "/planning/daily-focus" => "daily-focus",
            "/planning/backlog" => "backlog",
            "/planning/iteration" => "iteration",
            "/planning/repos" => "repos",
            _ => "daily-focus",
        };
    }

    /// <summary>Returns whether the URI is a Planning route.</summary>
    /// <param name="uri">The navigation URI.</param>
    /// <returns><see langword="true" /> when the path is under <c>/planning</c>; otherwise, <see langword="false" />.</returns>
    internal static bool IsPlanningPath(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return uri.Contains("/planning", StringComparison.OrdinalIgnoreCase);
        }

        return absolute.AbsolutePath.StartsWith("/planning", StringComparison.OrdinalIgnoreCase);
    }
}
