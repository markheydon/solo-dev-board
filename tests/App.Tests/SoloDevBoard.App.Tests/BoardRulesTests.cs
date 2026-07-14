using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.BoardRules.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="BoardRules"/> page.</summary>
public sealed class BoardRulesTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<IBoardRulesService> _boardRulesServiceMock = new();

    [Fact]
    public async Task BoardRules_WhileRepositoryServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(repositoriesTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();

        // Assert
        Assert.Contains("Loading repositories", cut.Markup);
    }

    [Fact]
    public async Task BoardRules_InitialLoad_ShowsEmptyVisualisationPrompt()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select a repository and project board", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-empty-state']"));
        });
    }

    [Fact]
    public async Task BoardRules_PageLayout_RendersRepositorySelectorBeforeVisualisationRegion()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-selector-region']"));
            Assert.Single(cut.FindAll("[data-testid='board-rules-visualisation-region']"));

            var markup = cut.Markup;
            var selectorPosition = markup.IndexOf("board-rules-selector-region", StringComparison.Ordinal);
            var visualisationPosition = markup.IndexOf("board-rules-visualisation-region", StringComparison.Ordinal);
            Assert.True(selectorPosition >= 0);
            Assert.True(visualisationPosition > selectorPosition);
        });
    }

    [Fact]
    public async Task BoardRules_RepositorySelected_LoadsSupportedProjectBoards()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Board", cut.Markup);
            Assert.Contains("Board context ready", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-board-context-ready-state']"));
        });

        _boardRulesServiceMock.Verify(
            service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BoardRules_NoSupportedProjectBoards_ShowsUnsupportedMessageAndBlocksDiagramState()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-unsupported-boards-message']"));
            Assert.Single(cut.FindAll("[data-testid='board-rules-no-supported-board-state']"));
            Assert.DoesNotContain("Board context ready", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_RepositoryLoadFailure_ShowsRetryAction()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub unavailable"));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load repositories", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-reload-repositories-button']"));
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _boardRulesServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoryAsync(IRenderedComponent<BoardRules> cut, RepositoryDto repository)
    {
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync([repository.FullName]));
    }

    private static RepositoryDto CreateRepository(string owner, string name)
        => new(
            Id: 0,
            Name: name,
            FullName: $"{owner}/{name}",
            Description: string.Empty,
            Url: string.Empty,
            IsPrivate: false,
            IsArchived: false,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
