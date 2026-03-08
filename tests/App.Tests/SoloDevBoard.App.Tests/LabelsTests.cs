using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Labels"/> page.</summary>
public sealed class LabelsTests : BunitContext
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<ILabelManagerService> _labelManagerServiceMock = new();

    /// <summary>Initialises the bUnit test context for Fluent UI components and mocked services.</summary>
    public LabelsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
        Services.AddScoped(_ => _repositoryServiceMock.Object);
        Services.AddScoped(_ => _labelManagerServiceMock.Object);
    }

    [Fact]
    public void Labels_WhileServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<Repository>>();
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(repositoriesTask.Task);

        // Act
        var cut = Render<Labels>();

        // Assert
        Assert.Contains("Loading labels", cut.Markup);
    }

    [Fact]
    public void Labels_ServiceReturnsNoLabels_ShowsEmptyState()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository { Name = "repo-a", FullName = "owner/repo-a" },
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

        // Act
        var cut = Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No labels found", cut.Markup);
            Assert.Contains("No labels were returned for the selected repositories", cut.Markup);
        });
    }

    [Fact]
    public void Labels_RepositoriesHavePartialLabels_ShowsGapAnalysisInGrid()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository { Name = "repo-a", FullName = "owner/repo-a" },
                new Repository { Name = "repo-b", FullName = "owner/repo-b" },
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-a"),
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-b"),
            ]);

        // Act
        var cut = Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("type/story", cut.Markup);
            Assert.Contains("Story label", cut.Markup);
            Assert.Contains("repo-a", cut.Markup);
            Assert.Contains("repo-b", cut.Markup);
            Assert.Contains("Missing In", cut.Markup);
            Assert.Contains("priority/high", cut.Markup);
        });
    }

    [Fact]
    public void Labels_FilterApplied_FiltersRowsByName()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository { Name = "repo-a", FullName = "owner/repo-a" },
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("status/done", "cfd3d7", "Completed", "repo-a"),
            ]);

        var cut = Render<Labels>();

        cut.WaitForAssertion(() => Assert.Contains("type/story", cut.Markup));

        // Act
        var filter = cut.Find("[data-testid='label-filter']");
        filter.Input("status");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("status/done", cut.Markup);
            Assert.DoesNotContain("type/story", cut.Markup);
        });
    }
}
