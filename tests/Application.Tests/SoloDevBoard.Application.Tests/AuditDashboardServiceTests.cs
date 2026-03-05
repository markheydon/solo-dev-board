using Moq;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Tests;

public sealed class AuditDashboardServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();
    private readonly AuditDashboardService _sut;

    public AuditDashboardServiceTests()
    {
        _sut = new AuditDashboardService(_gitHubServiceMock.Object);
    }

    [Fact]
    public async Task GetRepositorySummaryAsync_ShouldReturnRepositories_FromGitHubService()
    {
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one" },
            new() { Id = 2, Name = "repo-two", FullName = "owner/repo-two" },
        };
        _gitHubServiceMock
            .Setup(s => s.GetRepositoriesAsync("owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRepositories);

        // Act
        var result = await _sut.GetRepositorySummaryAsync("owner");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("repo-one", result[0].Name);
    }

    [Fact]
    public async Task GetOpenIssuesAsync_ShouldReturnIssues_FromGitHubService()
    {
        // Arrange
        var expectedIssues = new List<Issue>
        {
            new() { Id = 10, Number = 1, Title = "First issue", State = "open" },
        };
        _gitHubServiceMock
            .Setup(s => s.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedIssues);

        // Act
        var result = await _sut.GetOpenIssuesAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal("First issue", result[0].Title);
    }
}
