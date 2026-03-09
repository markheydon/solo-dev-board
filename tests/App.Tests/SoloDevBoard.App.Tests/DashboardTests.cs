using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;

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
    public void Dashboard_WhenRendered_DisplaysAllSixFeaturePanels()
    {
        // Arrange

        // Act
        var cut = Render<Dashboard>();

        // Assert
        Assert.Contains("Audit Dashboard", cut.Markup);
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

        Assert.Equal(6, links.Count);
        Assert.Contains("/audit", links);
        Assert.Contains("/migrate", links);
        Assert.Contains("/labels", links);
        Assert.Contains("/board-rules", links);
        Assert.Contains("/triage", links);
        Assert.Contains("/workflows", links);
    }
}
