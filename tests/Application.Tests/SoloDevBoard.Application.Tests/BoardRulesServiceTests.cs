using Moq;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.GitHub;

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
}
