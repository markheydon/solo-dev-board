using NSubstitute;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BoardRulesService"/>.</summary>
public sealed class BoardRulesServiceTests
{
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public void Constructor_GitHubServiceIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IGitHubService? gitHubService = null;

        // Act
        var action = () => _ = new BoardRulesService(gitHubService!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetBoardRulesAsync_ValidRequest_ReturnsDefinitionFromGitHubService()
    {
        // Arrange
        var expected = new BoardRulesDefinitionDto(
            "PVT_kwHOAJefG84BQ6bh",
            "Roadmap",
            "owner",
            Array.Empty<BoardColumnDto>(),
            Array.Empty<BoardRuleDto>(),
            new[] { "Board automation rules are not yet available through the current GitHub query model." });

        _gitHubService
            .GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", Arg.Any<CancellationToken>())
            .Returns(expected);

        var sut = new BoardRulesService(_gitHubService);

        // Act
        var result = await sut.GetBoardRulesAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh");

        // Assert
        Assert.Same(expected, result);
        await _gitHubService.Received(1).GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_ProjectBoardsReturned_ReturnsSortedOptions()
    {
        // Arrange
        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo", Arg.Any<CancellationToken>())
            .Returns(new RepositoryProjectBoardDiscoveryResult(
            [
                new TriageProjectBoard
                {
                    Id = "PVT_beta",
                    Title = "Beta Board",
                    OwnerLogin = "owner",
                    StatusFieldId = "status-field",
                    StatusOptions = [],
                },
                new TriageProjectBoard
                {
                    Id = "PVT_alpha",
                    Title = "Alpha Board",
                    OwnerLogin = "owner",
                    StatusFieldId = "status-field",
                    StatusOptions = [],
                },
            ],
            2,
            0));

        var sut = new BoardRulesService(_gitHubService);

        // Act
        var result = await sut.GetProjectBoardOptionsAsync("owner", "repo");

        // Assert
        Assert.Equal(2, result.Options.Count);
        Assert.Equal(2, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);
        Assert.Equal("PVT_alpha", result.Options[0].Id);
        Assert.Equal("Alpha Board", result.Options[0].Title);
        Assert.Equal("owner", result.Options[0].OwnerLogin);
        Assert.Equal("PVT_beta", result.Options[1].Id);
        await _gitHubService.Received(1).GetProjectBoardsForRepositoryAsync("owner", "repo", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new BoardRulesService(_gitHubService);

        // Act
        var action = () => sut.GetProjectBoardOptionsAsync(" ", "repo");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetBoardRulesAsync_ProjectIdIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new BoardRulesService(_gitHubService);

        // Act
        var action = () => sut.GetBoardRulesAsync("owner", "repo", " ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_NoSupportedBoards_ReturnsEmptyOptions()
    {
        // Arrange
        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo", Arg.Any<CancellationToken>())
            .Returns(new RepositoryProjectBoardDiscoveryResult([], 0, 0));

        var sut = new BoardRulesService(_gitHubService);

        // Act
        var result = await sut.GetProjectBoardOptionsAsync("owner", "repo");

        // Assert
        Assert.Empty(result.Options);
        Assert.Equal(0, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_PartiallyAccessibleBoards_PropagatesVisibilityCounts()
    {
        // Arrange
        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo", Arg.Any<CancellationToken>())
            .Returns(new RepositoryProjectBoardDiscoveryResult(
            [
                new TriageProjectBoard
                {
                    Id = "PVT_alpha",
                    Title = "Alpha Board",
                    OwnerLogin = "owner",
                    StatusFieldId = "status-field",
                    StatusOptions = [],
                },
            ],
            3,
            2));

        var sut = new BoardRulesService(_gitHubService);

        // Act
        var result = await sut.GetProjectBoardOptionsAsync("owner", "repo");

        // Assert
        Assert.Single(result.Options);
        Assert.Equal(3, result.TotalLinkedProjectCount);
        Assert.Equal(2, result.InaccessibleLinkedProjectCount);
    }

    [Fact]
    public void CompareBoardRules_TwoDefinitions_ReturnsComparisonResult()
    {
        // Arrange
        var left = new BoardRulesDefinitionDto(
            "PVT_alpha",
            "Alpha Board",
            "owner",
            [new BoardColumnDto(0, "To Do", 0, ["To Do"])],
            [],
            []);
        var right = new BoardRulesDefinitionDto(
            "PVT_beta",
            "Beta Board",
            "owner",
            [new BoardColumnDto(0, "Backlog", 0, ["Backlog"])],
            [],
            []);

        var sut = new BoardRulesService(_gitHubService);

        // Act
        var result = sut.CompareBoardRules(left, right);

        // Assert
        Assert.True(result.HasDifferences);
        Assert.Equal(left, result.LeftDefinition);
        Assert.Equal(right, result.RightDefinition);
    }
}
