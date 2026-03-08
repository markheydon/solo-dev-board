using Moq;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="LabelManagerService"/>.</summary>
public sealed class LabelManagerServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();

    [Fact]
    public async Task GetLabelsAsync_GitHubServiceReturnsLabels_ReturnsMappedLabelDtos()
    {
        // Arrange
        var expectedLabels = new List<Label>
        {
            new()
            {
                Name = "type/story",
                Colour = "1d76db",
                Description = "A user-facing Story delivering a discrete piece of value",
                RepositoryName = "solo-dev-board",
            },
            new()
            {
                Name = "priority/high",
                Colour = "d93f0b",
                Description = "Should be addressed in the current sprint or release",
                RepositoryName = "solo-dev-board",
            },
        };

        _gitHubServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLabels);

        var sut = new LabelManagerService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetLabelsAsync("owner", "repo");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("type/story", result[0].Name);
        Assert.Equal("1d76db", result[0].Colour);
        Assert.Equal("A user-facing Story delivering a discrete piece of value", result[0].Description);
        Assert.Equal("solo-dev-board", result[0].RepositoryName);

        Assert.Equal("priority/high", result[1].Name);
        Assert.Equal("d93f0b", result[1].Colour);
        Assert.Equal("Should be addressed in the current sprint or release", result[1].Description);
        Assert.Equal("solo-dev-board", result[1].RepositoryName);
    }

    [Fact]
    public async Task GetLabelsAsync_WhenCalled_PassesCancellationTokenToGitHubService()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        _gitHubServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", cancellationTokenSource.Token))
            .ReturnsAsync([]);

        var sut = new LabelManagerService(_gitHubServiceMock.Object);

        // Act
        _ = await sut.GetLabelsAsync("owner", "repo", cancellationTokenSource.Token);

        // Assert
        _gitHubServiceMock.Verify(service => service.GetLabelsAsync("owner", "repo", cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task SyncLabelsAsync_WhenCalled_CompletesWithoutError()
    {
        // Arrange
        var sut = new LabelManagerService(_gitHubServiceMock.Object);

        // Act
        var task = sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo");
        await task;

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
    }
}
