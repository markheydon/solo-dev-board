using Moq;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BoardRulesService"/>.</summary>
public sealed class BoardRulesServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();

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

        _gitHubServiceMock
            .Setup(service => service.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new BoardRulesService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetBoardRulesAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh");

        // Assert
        Assert.Same(expected, result);
        _gitHubServiceMock.Verify(service => service.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_ProjectBoardsReturned_ReturnsSortedOptions()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetProjectBoardsForRepositoryAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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
            ]);

        var sut = new BoardRulesService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetProjectBoardOptionsAsync("owner", "repo");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("PVT_alpha", result[0].Id);
        Assert.Equal("Alpha Board", result[0].Title);
        Assert.Equal("owner", result[0].OwnerLogin);
        Assert.Equal("PVT_beta", result[1].Id);
        _gitHubServiceMock.Verify(service => service.GetProjectBoardsForRepositoryAsync("owner", "repo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProjectBoardOptionsAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new BoardRulesService(_gitHubServiceMock.Object);

        // Act
        var action = () => sut.GetProjectBoardOptionsAsync(" ", "repo");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }
}
