using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using SoloDevBoard.App.Components.Pages;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Dashboard"/> page shell.</summary>
public sealed class DashboardTests : BunitContext
{
    /// <summary>
    /// Initialises Fluent UI services and loose JS interop so Fluent components render in bUnit.
    /// </summary>
    public DashboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
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
        var links = cut.FindAll("a.feature-link").Select(link => link.GetAttribute("href")).ToList();

        Assert.Equal(6, links.Count);
        Assert.Contains("/audit", links);
        Assert.Contains("/migrate", links);
        Assert.Contains("/labels", links);
        Assert.Contains("/board-rules", links);
        Assert.Contains("/triage", links);
        Assert.Contains("/workflows", links);
    }
}
