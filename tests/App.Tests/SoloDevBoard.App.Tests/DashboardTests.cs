using Bunit;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Dashboard.Pages;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Dashboard"/> page shell.</summary>
public sealed class DashboardTests : BunitContext
{
    /// <summary>
    /// Initialises MudBlazor services and loose JS interop so MudBlazor components render in bUnit.
    /// </summary>
    public DashboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Dashboard_WhenRendered_DisplaysAllSevenFeaturePanels()
    {
        // Arrange

        // Act
        var cut = Render<Dashboard>();

        // Assert
        Assert.Contains("Audit Dashboard", cut.Markup);
        Assert.Contains("Repositories", cut.Markup);
        Assert.Contains("One-Click Migration", cut.Markup);
        Assert.Contains("Label Manager", cut.Markup);
        Assert.Contains("Board Rules Visualiser", cut.Markup);
        Assert.Contains("Triage UI", cut.Markup);
        Assert.Contains("Workflow Templates", cut.Markup);
    }

    [Fact]
    public void Dashboard_WhenRendered_ContainsCorrectFeatureLinks()
    {
        // Arrange

        // Act
        var cut = Render<Dashboard>();

        // Assert
        var links = cut.FindAll("a")
            .Select(link => link.GetAttribute("href"))
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .ToList();

        Assert.Equal(7, links.Count);
        Assert.Contains("/audit-dashboard", links);
        Assert.Contains("/repositories", links);
        Assert.Contains("/migrate", links);
        Assert.Contains("/labels", links);
        Assert.Contains("/board-rules", links);
        Assert.Contains("/triage", links);
        Assert.Contains("/workflows", links);
    }
}
