using NSubstitute;
using SoloDevBoard.Domain.Entities;
using SoloDevBoard.Infrastructure;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly GitHubService _sut;

    public GitHubServiceTests()
    {
        _sut = new GitHubService(_httpClientFactory);
    }

    [Fact]
    public async Task GetRepositoriesAsync_ShouldReturnEmptyList_WhenStubImplementation()
    {
        // Act
        var result = await _sut.GetRepositoriesAsync("test-owner");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLabelsAsync_ShouldReturnEmptyList_WhenStubImplementation()
    {
        // Act
        var result = await _sut.GetLabelsAsync("test-owner", "test-repo");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateLabelAsync_ShouldReturnSameLabel_WhenStubImplementation()
    {
        // Arrange
        var label = new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working" };

        // Act
        var result = await _sut.CreateLabelAsync("test-owner", "test-repo", label);

        // Assert
        Assert.Equal(label, result);
    }
}
