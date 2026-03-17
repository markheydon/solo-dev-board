using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.About.Pages;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="About"/> page.</summary>
public sealed class AboutTests : BunitContext
{
    private const string TestVersion = "1.2.3";
    private readonly Mock<IAppVersionService> _appVersionServiceMock = new();

    /// <summary>Initialises MudBlazor services and test doubles for About page rendering.</summary>
    public AboutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        ConfigureVersionService();
        Services.AddSingleton(_appVersionServiceMock.Object);
    }

    private void ConfigureVersionService()
    {
        _appVersionServiceMock
            .Setup(service => service.Version)
            .Returns(TestVersion);

        _appVersionServiceMock
            .Setup(service => service.UserAgent)
            .Returns($"SoloDevBoard/{TestVersion}");
    }

    [Fact]
    public void AboutPage_RenderedWithMockedVersionService_RendersWithoutError()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void AboutPage_RenderedWithMockedVersionService_DisplaysVersionFromService()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains(TestVersion, cut.Markup);
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysRepositoryLink()
    {
        // Act
        var cut = Render<About>();

        // Assert
        var link = cut.Find("[data-testid='about-repository-link']");
        Assert.Equal("https://github.com/markheydon/solo-dev-board", link.GetAttribute("href"));
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysDotNetVersion()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains(Environment.Version.ToString(), cut.Markup);
    }
}
