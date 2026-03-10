using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="About"/> page.</summary>
public sealed class AboutTests : BunitContext
{
    private readonly Mock<IAppVersionService> _appVersionServiceMock = new();

    /// <summary>Initialises MudBlazor services and test doubles for About page rendering.</summary>
    public AboutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_appVersionServiceMock.Object);
    }

    [Fact]
    public void AboutPage_RenderedWithMockedVersionService_RendersWithoutError()
    {
        // Arrange
        _appVersionServiceMock
            .Setup(service => service.Version)
            .Returns("1.2.3");

        _appVersionServiceMock
            .Setup(service => service.UserAgent)
            .Returns("SoloDevBoard/1.2.3");

        // Act
        Exception? exception = null;
        try
        {
            _ = Render<About>();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AboutPage_RenderedWithMockedVersionService_DisplaysVersionFromService()
    {
        // Arrange
        _appVersionServiceMock
            .Setup(service => service.Version)
            .Returns("1.2.3");

        _appVersionServiceMock
            .Setup(service => service.UserAgent)
            .Returns("SoloDevBoard/1.2.3");

        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains("1.2.3", cut.Markup);
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysRepositoryLink()
    {
        // Arrange
        _appVersionServiceMock
            .Setup(service => service.Version)
            .Returns("1.2.3");

        _appVersionServiceMock
            .Setup(service => service.UserAgent)
            .Returns("SoloDevBoard/1.2.3");

        // Act
        var cut = Render<About>();

        // Assert
        var link = cut.Find("[data-testid='about-repository-link']");
        Assert.Equal("https://github.com/markheydon/solo-dev-board", link.GetAttribute("href"));
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysDotNetVersion()
    {
        // Arrange
        _appVersionServiceMock
            .Setup(service => service.Version)
            .Returns("1.2.3");

        _appVersionServiceMock
            .Setup(service => service.UserAgent)
            .Returns("SoloDevBoard/1.2.3");

        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains(Environment.Version.ToString(), cut.Markup);
    }
}
