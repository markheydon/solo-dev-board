using NSubstitute;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;
using SoloDevBoard.Infrastructure;

namespace SoloDevBoard.Application.Tests;

public sealed class AuditDashboardServiceTests
{
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();
    private readonly AuditDashboardService _sut;

    public AuditDashboardServiceTests()
    {
        _sut = new AuditDashboardService(_gitHubService);
    }

    [Fact]
    public async Task GetRepositorySummaryAsync_ShouldReturnRepositories_FromGitHubService()
    {
        // Arrange
        var expectedRepositories = new List<Repository>
        {
            new Repository { Id = 1, Name = "repo-one", FullName = "owner/repo-one" },
            new Repository { Id = 2, Name = "repo-two", FullName = "owner/repo-two" },
        };
        _gitHubService.GetRepositoriesAsync("owner", Arg.Any<CancellationToken>())
            .Returns(expectedRepositories);

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
            new Issue { Id = 10, Number = 1, Title = "First issue", State = "open" },
        };
        _gitHubService.GetIssuesAsync("owner", "repo", Arg.Any<CancellationToken>())
            .Returns(expectedIssues);

        // Act
        var result = await _sut.GetOpenIssuesAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal("First issue", result[0].Title);
    }
}
