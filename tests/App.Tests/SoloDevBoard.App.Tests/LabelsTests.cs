using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;
using System.Net.Http;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Labels"/> page.</summary>
public sealed class LabelsTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<ILabelManagerService> _labelManagerServiceMock = new();

    [Fact]
    public async Task Labels_WhileRepositoryServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(repositoriesTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        Assert.Contains("Loading repositories", cut.Markup);
    }

    [Fact]
    public async Task Labels_InitialLoad_DoesNotFetchLabelsUntilRequested()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Load selected repositories", cut.Markup);
            Assert.Contains("New label", cut.Markup);
            Assert.Contains("Showing 1 active repository", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='label-filter']"));
            Assert.Empty(cut.FindAll("[data-testid='labels-grid']"));
        });

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Labels_RepositoriesLoaded_ArchivedRepositoriesAreHiddenByDefault()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a", isArchived: false),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 1 active repository", cut.Markup);
            Assert.Contains("Archived repositories are hidden by default", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_LoadRequestedAndNoLabelsReturned_ShowsEmptyState()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

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
        var repoA = CreateRepository("owner-a", "repo-a");
        var repoB = CreateRepository("owner-b", "repo-b");

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

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

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
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("status/done", "cfd3d7", "Completed", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        cut.WaitForAssertion(() => Assert.Contains("type/story", cut.Markup));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='label-filter']")));
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("[data-testid='edit-label-button']").Count);
            Assert.Equal(2, cut.FindAll("[data-testid='delete-label-button']").Count);
        });

        var filterInputContainer = cut.Find("[data-testid='label-filter']");
        var filterTextInput = filterInputContainer.QuerySelector("input");
        Assert.NotNull(filterTextInput);

        // Act
        filterTextInput!.Input("status");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("status/done", cut.Markup);
            Assert.DoesNotContain("type/story", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_RepositoryLoadFails_ShowsRepositorySpecificErrorAndAction()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load repositories", cut.Markup);
            Assert.Contains("Try loading repositories again", cut.Markup);
            Assert.DoesNotContain("Try loading labels again", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _labelManagerServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Labels> cut, params RepositoryDto[] repositories)
    {
        var autocomplete = cut.FindComponent<MudAutocomplete<RepositoryDto>>();

        await cut.InvokeAsync(async () =>
        {
            foreach (var repository in repositories)
            {
                await autocomplete.Instance.ValueChanged.InvokeAsync(repository);
            }
        });
    }

    private static RepositoryDto CreateRepository(string owner, string name, bool isPrivate = false, bool isArchived = false)
        => new(
            Id: 0,
            Name: name,
            FullName: $"{owner}/{name}",
            Description: string.Empty,
            Url: string.Empty,
            IsPrivate: isPrivate,
            IsArchived: isArchived,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
