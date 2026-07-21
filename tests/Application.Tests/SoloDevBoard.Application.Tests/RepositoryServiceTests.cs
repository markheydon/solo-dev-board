using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Domain.Entities.Repositories;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="RepositoryService"/>.</summary>
public sealed class RepositoryServiceTests
{
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public async Task GetRepositoriesAsync_GitHubServiceReturnsRepositories_ReturnsRepositories()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one" },
            new() { Id = 2, Name = "repo-two", FullName = "owner/repo-two" },
        };

        _gitHubService
            .GetRepositoriesAsync(cancellationToken)
            .Returns(expectedRepositories);

        var sut = new RepositoryService(_gitHubService);

        // Act
        var result = await sut.GetRepositoriesAsync(cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("repo-one", result[0].Name);
        await _gitHubService.Received(1).GetRepositoriesAsync(cancellationToken);
    }

    [Fact]
    public async Task GetRepositoriesAsync_GitHubServiceReturnsRepository_MapsAllFieldsToRepositoryDto()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var createdAt = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

        _gitHubService
            .GetRepositoriesAsync(cancellationToken)
            .Returns([
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

        var sut = new RepositoryService(_gitHubService);

        // Act
        var result = await sut.GetRepositoriesAsync(cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _gitHubService
            .GetRepositoriesAsync(cancellationTokenSource.Token)
            .Returns([]);

        var sut = new RepositoryService(_gitHubService);

        // Act
        _ = await sut.GetRepositoriesAsync(cancellationTokenSource.Token);

        // Assert
        await _gitHubService.Received(1).GetRepositoriesAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_GitHubServiceReturnsRepositories_ReturnsRepositories()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one", IsArchived = false },
        };

        _gitHubService
            .GetActiveRepositoriesAsync(cancellationToken)
            .Returns(expectedRepositories);

        var sut = new RepositoryService(_gitHubService);

        // Act
        var result = await sut.GetActiveRepositoriesAsync(cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("repo-one", result[0].Name);
        await _gitHubService.Received(1).GetActiveRepositoriesAsync(cancellationToken);
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_WhenCalled_PassesCancellationTokenToGitHubService()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _gitHubService
            .GetActiveRepositoriesAsync(cancellationTokenSource.Token)
            .Returns([]);

        var sut = new RepositoryService(_gitHubService);

        // Act
        _ = await sut.GetActiveRepositoriesAsync(cancellationTokenSource.Token);

        // Assert
        await _gitHubService.Received(1).GetActiveRepositoriesAsync(cancellationTokenSource.Token);
    }
}
