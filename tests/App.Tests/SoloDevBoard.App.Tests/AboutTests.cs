using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Components.Features.About.Pages;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="About"/> page.</summary>
public sealed class AboutTests : BunitContext
{
    private const string TestVersion = "1.2.3";
    private const string TestBuildMetadata = "abc1234";
    private const string TestBuiltAtDisplay = "23 Aug 26 @ 15:11 BST";
    private readonly IAppVersionService _appVersionService = Substitute.For<IAppVersionService>();
    private readonly IGitHubAuthenticationSummaryService _authenticationSummaryService = Substitute.For<IGitHubAuthenticationSummaryService>();

    /// <summary>Initialises MudBlazor services and test doubles for About page rendering.</summary>
    public AboutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        ConfigureVersionService();
        ConfigureAuthenticationSummaryService();
        Services.AddSingleton(_appVersionService);
        Services.AddSingleton(_authenticationSummaryService);
    }

    private void ConfigureVersionService()
    {
        _appVersionService.Version.Returns(TestVersion);
        _appVersionService.BuildMetadata.Returns(TestBuildMetadata);
        _appVersionService.BuiltAtDisplay.Returns(string.Empty);
        _appVersionService.UserAgent.Returns($"SoloDevBoard/{TestVersion}");
    }

    private void ConfigureAuthenticationSummaryService()
    {
        _authenticationSummaryService
            .GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(new GitHubAuthenticationSummary(
                "PAT-only local trusted mode",
                "Connected as",
                "markheydon"));
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
    public void AboutPage_RenderedWithMockedVersionService_DisplaysBuildMetadataLink()
    {
        // Act
        var cut = Render<About>();

        // Assert
        var buildLink = cut.Find("[data-testid='about-build']");
        Assert.Equal(TestBuildMetadata, buildLink.TextContent.Trim());
        Assert.Equal(
            $"https://github.com/markheydon/solo-dev-board/commit/{TestBuildMetadata}",
            buildLink.GetAttribute("href"));
    }

    [Fact]
    public void AboutPage_RenderedWithoutBuildMetadata_HidesBuildRow()
    {
        // Arrange
        _appVersionService.BuildMetadata.Returns(string.Empty);

        // Act
        var cut = Render<About>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='about-build']"));
    }

    [Fact]
    public void AboutPage_RenderedWithBuiltAtDisplay_DisplaysBuiltRow()
    {
        // Arrange
        _appVersionService.BuiltAtDisplay.Returns(TestBuiltAtDisplay);

        // Act
        var cut = Render<About>();

        // Assert
        var builtAt = cut.Find("[data-testid='about-built-at']");
        Assert.Equal(TestBuiltAtDisplay, builtAt.TextContent.Trim());
    }

    [Fact]
    public void AboutPage_RenderedWithoutBuiltAtDisplay_HidesBuiltRow()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='about-built-at']"));
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysRepositoryLink()
    {
        // Act
        var cut = Render<About>();

        // Assert
        var link = cut.Find("[data-testid='about-repository-link']");
        Assert.Equal("https://github.com/markheydon/solo-dev-board", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysDotNetVersion()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains(Environment.Version.ToString(), cut.Markup);
    }

    [Fact]
    public void AboutPage_Rendered_DisplaysGitHubAuthenticationSummary()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Equal("PAT-only local trusted mode", cut.Find("[data-testid='about-auth-mode']").TextContent.Trim());
        Assert.Equal("@markheydon", cut.Find("[data-testid='about-github-login']").TextContent.Trim());
    }
}
