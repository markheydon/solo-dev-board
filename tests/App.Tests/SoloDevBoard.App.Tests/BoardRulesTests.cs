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
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ],
            2,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                Array.Empty<BoardRuleDto>(),
                ["Board automation rules are not yet available through the current GitHub query model."]));

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
        _boardRulesServiceMock.Verify(
            service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()),
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
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto([], 0, 0));

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
    public async Task BoardRules_InaccessibleLinkedProjectBoards_ShowsWarningAndSupportedBoards()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_public", "Public Board", "owner"),
            ],
            2,
            1));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_public",
                "Public Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "Done", 1, ["Done"]),
                ],
                Array.Empty<BoardRuleDto>(),
                []));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Public Board", cut.Markup);
            Assert.Contains("2 linked project boards", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1 board could not be loaded", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Single(cut.FindAll("[data-testid='board-rules-inaccessible-project-boards-warning']"));
        });
    }

    [Fact]
    public async Task BoardRules_SelectedBoard_RendersTransitionsAndDetailPanel()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                    new BoardColumnDto(2, "Done", 2, ["Done"]),
                ],
                [
                    new BoardRuleDto(1, "Auto-assign PRs", "When pull request opened", "Assign reviewer", true),
                    new BoardRuleDto(2, "Auto-close stale", "When issue is stale", "Close issue", true),
                ],
                ["Board automation rules are not yet available through the current GitHub query model."]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Auto-assign PRs", cut.Markup);
            Assert.Contains("Auto-close stale", cut.Markup);
            Assert.Contains("Transition detail", cut.Markup);
            Assert.Contains("From:", cut.Markup);
            Assert.Contains("To:", cut.Markup);
        });

        var ruleChip = cut.Find("[data-testid='board-rules-rule-chip-1']");
        ruleChip.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Selected rule", cut.Markup);
            Assert.Contains("Auto-assign PRs", cut.Markup);
            Assert.Contains("When pull request opened", cut.Markup);
            Assert.Contains("Assign reviewer", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_ChangingProjectBoard_ClearsSelectedRuleDetail()
    {
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ],
            2,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                [
                    new BoardRuleDto(1, "Auto-assign", "When issue added", "Assign reviewer", true),
                ],
                []));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_beta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_beta",
                "Beta Board",
                "owner",
                [
                    new BoardColumnDto(0, "Backlog", 0, ["Backlog"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                [
                    new BoardRuleDto(2, "Auto-close stale", "When issue is stale", "Close issue", true),
                ],
                []));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        cut.WaitForAssertion(() => Assert.Contains("Auto-assign", cut.Markup));
        var ruleChip = cut.Find("[data-testid='board-rules-rule-chip-1']");
        ruleChip.Click();
        cut.WaitForAssertion(() => Assert.Contains("Selected rule", cut.Markup));

        var boardSelect = cut.FindComponents<MudSelect<string>>().Single();
        await cut.InvokeAsync(() => boardSelect.Instance.ValueChanged.InvokeAsync("PVT_beta"));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Auto-assign", cut.Markup);
            Assert.Contains("Auto-close stale", cut.Markup);
            Assert.Contains("Transition detail", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_ChangingRepository_ClearsSelectedRuleDetail()
    {
        var repositoryA = CreateRepository("owner", "repo-a");
        var repositoryB = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repositoryA, repositoryB]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                [
                    new BoardRuleDto(1, "Auto-assign", "When issue added", "Assign reviewer", true),
                ],
                []));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-b", "PVT_beta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_beta",
                "Beta Board",
                "owner",
                [
                    new BoardColumnDto(0, "Backlog", 0, ["Backlog"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                [
                    new BoardRuleDto(2, "Auto-close stale", "When issue is stale", "Close issue", true),
                ],
                []));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repositoryA);

        cut.WaitForAssertion(() => Assert.Contains("Auto-assign", cut.Markup));
        var ruleChip = cut.Find("[data-testid='board-rules-rule-chip-1']");
        ruleChip.Click();
        cut.WaitForAssertion(() => Assert.Contains("Selected rule", cut.Markup));

        await SelectRepositoryAsync(cut, repositoryB);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Auto-assign", cut.Markup);
            Assert.Contains("Auto-close stale", cut.Markup);
            Assert.Contains("Transition detail", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_RuleWarnings_DisplaysConflictWarningAndHighlightsRules()
    {
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "In Progress", 1, ["In Progress"]),
                ],
                [
                    new BoardRuleDto(1, "Auto-assign", "When item added", string.Empty, true),
                    new BoardRuleDto(2, "Auto-assign duplicate", "When item added", "Assign reviewer", true),
                ],
                ["Board automation rules are not yet available through the current GitHub query model."]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Potential rule conflicts detected", cut.Markup);
            Assert.Contains("Rules with the same trigger 'When item added' may conflict", cut.Markup);
            Assert.Contains("Rule 'Auto-assign' has incomplete configuration", cut.Markup);
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

    [Fact]
    public async Task BoardRules_NoActiveRepositories_ShowsEmptyRepositoriesState()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<BoardRules>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-repositories-empty-state']"));
            Assert.Contains("No active repositories are available", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_ProjectBoardLoadFailure_ShowsRetryAction()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load project boards", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-reload-boards-button']"));
        });
    }

    [Fact]
    public async Task BoardRules_BoardRulesLoadFailure_ShowsRetryAction()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load board rules", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-reload-boards-button']"));
        });
    }

    [Fact]
    public async Task BoardRules_WhileProjectBoardsAreLoading_ShowsDiagramLoadingState()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");
        var projectBoardsTask = new TaskCompletionSource<BoardRulesProjectBoardDiscoveryDto>();

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .Returns(projectBoardsTask.Task);

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        var selectTask = SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.True(
                cut.FindAll("[data-testid='board-rules-diagram-loading-state']").Count > 0
                || cut.FindAll("[data-testid='board-rules-boards-loading-state']").Count > 0);
        });

        projectBoardsTask.SetResult(new BoardRulesProjectBoardDiscoveryDto([], 0, 0));
        await selectTask;
    }

    [Fact]
    public async Task BoardRules_SupportedBoardsWithoutSelection_ShowsBoardNotSelectedState()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ],
            2,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [new BoardColumnDto(0, "To Do", 0, ["To Do"])],
                [],
                []));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-board-context-ready-state']"));
            Assert.Contains("Alpha Board", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_NoRuleWarnings_ShowsNeutralDiagramWithoutConflictAlert()
    {
        // Arrange
        var repository = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesDefinitionDto(
                "PVT_alpha",
                "Alpha Board",
                "owner",
                [
                    new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                    new BoardColumnDto(1, "Done", 1, ["Done"]),
                ],
                [
                    new BoardRuleDto(1, "Healthy rule", "When item added", "Assign reviewer", true),
                ],
                []));

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-repository-autocomplete']"));
        await SelectRepositoryAsync(cut, repository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Potential rule conflicts detected", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-board-context-ready-state']"));
        });
    }

    [Fact]
    public async Task BoardRules_CompareModeEnabled_ShowsComparisonSelector()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-compare-mode-toggle']"));

        // Act
        var compareSwitch = cut.FindComponent<MudSwitch<bool>>();
        await cut.InvokeAsync(() => compareSwitch.Instance.ValueChanged.InvokeAsync(true));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-comparison-selector-region']"));
            Assert.Contains("Comparison repository and project board", cut.Markup);
        });
    }

    [Fact]
    public async Task BoardRules_CompareModeWithTwoBoards_RendersComparisonDifferences()
    {
        // Arrange
        var repositoryA = CreateRepository("owner", "repo-a");
        var repositoryB = CreateRepository("owner", "repo-b");
        var primaryDefinition = new BoardRulesDefinitionDto(
            "PVT_alpha",
            "Alpha Board",
            "owner",
            [
                new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                new BoardColumnDto(1, "Done", 1, ["Done"]),
            ],
            [
                new BoardRuleDto(1, "Auto-assign", "When item added", "Assign reviewer", true),
            ],
            []);
        var comparisonDefinition = new BoardRulesDefinitionDto(
            "PVT_beta",
            "Beta Board",
            "owner",
            [
                new BoardColumnDto(0, "Backlog", 0, ["Backlog"]),
                new BoardColumnDto(1, "Done", 1, ["Done"]),
            ],
            [],
            []);
        var comparisonResult = new BoardRulesComparisonResultDto(
            primaryDefinition,
            comparisonDefinition,
            [
                new BoardRulesComparisonDifferenceDto(
                    "Column",
                    "Missing in comparison board",
                    "To Do",
                    "Column 'To Do' exists on the primary board but not on the comparison board."),
            ]);

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repositoryA, repositoryB]);

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_alpha", "Alpha Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetProjectBoardOptionsAsync("owner", "repo-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardRulesProjectBoardDiscoveryDto(
            [
                new BoardRulesProjectBoardOptionDto("PVT_beta", "Beta Board", "owner"),
            ],
            1,
            0));

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-a", "PVT_alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(primaryDefinition);

        _boardRulesServiceMock
            .Setup(service => service.GetBoardRulesAsync("owner", "repo-b", "PVT_beta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(comparisonDefinition);

        _boardRulesServiceMock
            .Setup(service => service.CompareBoardRules(primaryDefinition, comparisonDefinition))
            .Returns(comparisonResult);

        await using var ctx = CreateContext();
        var cut = ctx.Render<BoardRules>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='board-rules-compare-mode-toggle']"));

        var compareSwitch = cut.FindComponent<MudSwitch<bool>>();
        await cut.InvokeAsync(() => compareSwitch.Instance.ValueChanged.InvokeAsync(true));

        await SelectRepositoryAsync(cut, repositoryA);
        cut.WaitForAssertion(() => Assert.Contains("Alpha Board", cut.Markup));

        await SelectComparisonRepositoryAsync(cut, repositoryB);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='board-rules-comparison-results']"));
            Assert.Contains("Differences detected", cut.Markup);
            Assert.Contains("Column 'To Do' exists on the primary board but not on the comparison board.", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='board-rules-comparison-primary-summary']"));
            Assert.Single(cut.FindAll("[data-testid='board-rules-comparison-secondary-summary']"));
        });

        _boardRulesServiceMock.Verify(
            service => service.CompareBoardRules(primaryDefinition, comparisonDefinition),
            Times.Once);
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestHostedAuthenticationRecovery();
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

    private static async Task SelectComparisonRepositoryAsync(IRenderedComponent<BoardRules> cut, RepositoryDto repository)
    {
        var selectors = cut.FindComponents<RepositorySelector>();
        var comparisonSelector = selectors[^1];
        await cut.InvokeAsync(() => comparisonSelector.Instance.SelectedRepositoriesChanged.InvokeAsync([repository.FullName]));
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
