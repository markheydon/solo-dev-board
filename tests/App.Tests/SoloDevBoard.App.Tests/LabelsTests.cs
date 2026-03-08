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
    public void Labels_WhileRepositoryServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<Repository>>();
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(repositoriesTask.Task);

        // Act
        var cut = Render<Labels>();

        // Assert
        Assert.Contains("Loading repositories", cut.Markup);
    }

    [Fact]
    public void Labels_InitialLoad_DoesNotFetchLabelsUntilRequested()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository { Name = "repo-a", FullName = "owner/repo-a" },
            ]);

        // Act
        var cut = Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Load selected repositories", cut.Markup);
            Assert.Contains("Showing 1 active repositories", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='label-filter']"));
            Assert.Empty(cut.FindAll("[data-testid='labels-initial-state']"));
        });

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Labels_RepositoriesLoaded_ArchivedRepositoriesAreHiddenByDefault()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository { Name = "repo-a", FullName = "owner/repo-a", IsArchived = false },
            ]);

        // Act
        var cut = Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 1 active repositories", cut.Markup);
            Assert.Contains("Archived repositories are hidden by default", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_LoadRequestedAndNoLabelsReturned_ShowsEmptyState()
    {
        // Arrange
        var repoA = new Repository { Name = "repo-a", FullName = "owner/repo-a" };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

        // Act
        var cut = Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.FindComponent<FluentAutocomplete<Repository>>());

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No labels found", cut.Markup);
            Assert.Contains("No labels were returned for the selected repositories", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_SelectedRepositoriesAcrossOwners_LoadsEachOwnerAndShowsGapAnalysis()
    {
        // Arrange
        var repoA = new Repository { Name = "repo-a", FullName = "owner-a/repo-a" };
        var repoB = new Repository { Name = "repo-b", FullName = "owner-b/repo-b" };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA, repoB]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner-a", It.Is<IReadOnlyList<string>>(repositories => repositories.SequenceEqual(new[] { "repo-a" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-a"),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner-b", It.Is<IReadOnlyList<string>>(repositories => repositories.SequenceEqual(new[] { "repo-b" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-b"),
            ]);

        // Act
        var cut = Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.FindComponent<FluentAutocomplete<Repository>>());

        await SelectRepositoriesAsync(cut, repoA, repoB);
        cut.Find("[data-testid='load-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("type/story", cut.Markup);
            Assert.Contains("Story label", cut.Markup);
            Assert.Contains("owner-a/repo-a", cut.Markup);
            Assert.Contains("owner-b/repo-b", cut.Markup);
        });

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync("owner-a", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync("owner-b", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Labels_FilterAppliedAfterLoad_FiltersRowsByName()
    {
        // Arrange
        var repoA = new Repository { Name = "repo-a", FullName = "owner/repo-a" };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("status/done", "cfd3d7", "Completed", "repo-a"),
            ]);

        // Act
        var cut = Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.FindComponent<FluentAutocomplete<Repository>>());

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        cut.WaitForAssertion(() => Assert.Contains("type/story", cut.Markup));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='label-filter']")));

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

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Labels> cut, params Repository[] repositories)
    {
        var autocomplete = cut.FindComponent<FluentAutocomplete<Repository>>();

        await cut.InvokeAsync(async () =>
        {
            await autocomplete.Instance.SelectedOptionsChanged.InvokeAsync(repositories);
        });
    }
}
