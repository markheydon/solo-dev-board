using Moq;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Domain.Entities.BoardRules;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="RepositoryService"/>.</summary>
public sealed class RepositoryServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();

    [Fact]
    public async Task GetRepositoriesAsync_GitHubServiceReturnsRepositories_ReturnsRepositories()
    {
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one" },
            new() { Id = 2, Name = "repo-two", FullName = "owner/repo-two" },
        };

        _gitHubServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRepositories);

        var sut = new RepositoryService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetRepositoriesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("repo-one", result[0].Name);
        _gitHubServiceMock.Verify(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRepositoriesAsync_GitHubServiceReturnsRepository_MapsAllFieldsToRepositoryDto()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

        _gitHubServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Repository
                {
                    Id = 42,
                    Name = "repo-a",
                    FullName = "owner/repo-a",
                    Description = "Repository description",
                    Url = "https://github.com/owner/repo-a",
                    IsPrivate = true,
                    IsArchived = false,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                },
            ]);

        var sut = new RepositoryService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetRepositoriesAsync();

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(42, dto.Id);
        Assert.Equal("repo-a", dto.Name);
        Assert.Equal("owner/repo-a", dto.FullName);
        Assert.Equal("Repository description", dto.Description);
        Assert.Equal("https://github.com/owner/repo-a", dto.Url);
        Assert.True(dto.IsPrivate);
        Assert.False(dto.IsArchived);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal(updatedAt, dto.UpdatedAt);
    }

    [Fact]
    public async Task GetRepositoriesAsync_WhenCalled_PassesCancellationTokenToGitHubService()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _gitHubServiceMock
            .Setup(service => service.GetRepositoriesAsync(cancellationTokenSource.Token))
            .ReturnsAsync([]);

        var sut = new RepositoryService(_gitHubServiceMock.Object);

        // Act
        _ = await sut.GetRepositoriesAsync(cancellationTokenSource.Token);

        // Assert
        _gitHubServiceMock.Verify(service => service.GetRepositoriesAsync(cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_GitHubServiceReturnsRepositories_ReturnsRepositories()
    {
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one", IsArchived = false },
        };

        _gitHubServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRepositories);

        var sut = new RepositoryService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.GetActiveRepositoriesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("repo-one", result[0].Name);
        _gitHubServiceMock.Verify(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_WhenCalled_PassesCancellationTokenToGitHubService()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _gitHubServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(cancellationTokenSource.Token))
            .ReturnsAsync([]);

        var sut = new RepositoryService(_gitHubServiceMock.Object);

        // Act
        _ = await sut.GetActiveRepositoriesAsync(cancellationTokenSource.Token);

        // Assert
        _gitHubServiceMock.Verify(service => service.GetActiveRepositoriesAsync(cancellationTokenSource.Token), Times.Once);
    }
}
