using Moq;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Tests;

public sealed class AuditDashboardServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserContextMock = new();
    private readonly AuditDashboardService _sut;

    public AuditDashboardServiceTests()
    {
        _currentUserContextMock.SetupGet(context => context.OwnerLogin).Returns("owner");
        _sut = new AuditDashboardService(_gitHubServiceMock.Object, _currentUserContextMock.Object);
    }

    [Fact]
    public async Task GetRepositorySummaryAsync_ActiveRepositoriesExist_ReturnsRepositoryAuditSummaryDtos()
    {
        // Arrange
        var repositories = new List<Repository>
        {
            new() { Id = 1, Name = "repo-one", FullName = "owner/repo-one" },
            new() { Id = 2, Name = "repo-two", FullName = "owner/repo-two" },
        };
        _gitHubServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync("owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _gitHubServiceMock
            .Setup(service => service.GetWorkflowRunsAsync("owner", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetRepositorySummaryAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, summary => summary.RepositoryFullName == "owner/repo-one");
        Assert.Contains(result, summary => summary.RepositoryFullName == "owner/repo-two");
    }

    [Fact]
    public async Task GetOpenIssuesAsync_ValidRepo_ReturnsMappedIssueDtos()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new() { Id = 1, Number = 7, Title = "First issue", HtmlUrl = "https://example/issue/7", UpdatedAt = DateTimeOffset.UtcNow },
        };
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(issues);

        // Act
        var result = await _sut.GetOpenIssuesAsync("repo");

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(7, dto.Number);
        Assert.Equal("First issue", dto.Title);
        Assert.Equal("https://example/issue/7", dto.HtmlUrl);
        Assert.Equal("owner/repo", dto.RepositoryFullName);
    }

    [Fact]
    public async Task GetAuditSummaryAsync_ReposProvided_ReturnsPerRepositoryCounts()
    {
        // Arrange
        var repos = new[] { "repo-one" };
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo-one", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Issue { Id = 1, Number = 1, Labels = [] },
                new Issue { Id = 2, Number = 2, Labels = [new Label { Name = "bug" }] },
            ]);
        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo-one", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PullRequest { Id = 1, Number = 11, State = "open", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-20) },
                new PullRequest { Id = 2, Number = 12, State = "open", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            ]);
        _gitHubServiceMock
            .Setup(service => service.GetWorkflowRunsAsync("owner", "repo-one", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new WorkflowRun { Id = 1, WorkflowName = "build", Conclusion = "failure", UpdatedAt = DateTimeOffset.UtcNow },
            ]);

        // Act
        var result = await _sut.GetAuditSummaryAsync(repos);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal("owner/repo-one", summary.RepositoryFullName);
        Assert.Equal(2, summary.OpenIssueCount);
        Assert.Equal(2, summary.OpenPullRequestCount);
        Assert.Equal(1, summary.UnlabelledIssueCount);
        Assert.Equal(1, summary.StalePullRequestCount);
        Assert.Equal(1, summary.FailingWorkflowCount);
    }

    [Fact]
    public async Task GetUnlabelledIssuesAsync_UnlabelledIssuesExist_ReturnsOnlyUnlabelledIssues()
    {
        // Arrange
        var repos = new[] { "repo" };
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Issue { Id = 1, Number = 1, Title = "No labels", HtmlUrl = "https://example/1", Labels = [] },
                new Issue { Id = 2, Number = 2, Title = "Has labels", HtmlUrl = "https://example/2", Labels = [new Label { Name = "bug" }] },
            ]);

        // Act
        var result = await _sut.GetUnlabelledIssuesAsync(repos);

        // Assert
        var issue = Assert.Single(result);
        Assert.Equal(1, issue.Number);
        Assert.Equal("owner/repo", issue.RepositoryFullName);
    }

    [Fact]
    public async Task GetStalePullRequestsAsync_StalePullRequestsExist_ReturnsOnlyStalePullRequests()
    {
        // Arrange
        var repos = new[] { "repo" };
        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PullRequest { Id = 1, Number = 11, Title = "Stale", HtmlUrl = "https://example/pr/11", State = "open", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-30) },
                new PullRequest { Id = 2, Number = 12, Title = "Fresh", HtmlUrl = "https://example/pr/12", State = "open", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            ]);

        // Act
        var result = await _sut.GetStalePullRequestsAsync(repos, staleDays: 14);

        // Assert
        var pullRequest = Assert.Single(result);
        Assert.Equal(11, pullRequest.Number);
        Assert.Equal("owner/repo", pullRequest.RepositoryFullName);
    }

    [Fact]
    public async Task GetFailingWorkflowRunsAsync_MostRecentRunFails_ReturnsWorkflowRunDto()
    {
        // Arrange
        var repos = new[] { "repo" };
        _gitHubServiceMock
            .Setup(service => service.GetWorkflowRunsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new WorkflowRun
                {
                    Id = 1,
                    WorkflowName = "build",
                    Status = "completed",
                    Conclusion = "success",
                    HtmlUrl = "https://example/run/1",
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                },
                new WorkflowRun
                {
                    Id = 2,
                    WorkflowName = "build",
                    Status = "completed",
                    Conclusion = "failure",
                    HtmlUrl = "https://example/run/2",
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                },
            ]);

        // Act
        var result = await _sut.GetFailingWorkflowRunsAsync(repos);

        // Assert
        var workflowRun = Assert.Single(result);
        Assert.Equal("build", workflowRun.WorkflowName);
        Assert.Equal("failure", workflowRun.Conclusion);
        Assert.Equal("owner/repo", workflowRun.RepositoryFullName);
    }

    [Fact]
    public async Task GetStalePullRequestsAsync_StaleDaysIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repos = new[] { "repo" };

        // Act
        var act = async () => await _sut.GetStalePullRequestsAsync(repos, staleDays: 0);

        // Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetAuditSummaryAsync_ReposIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<string> repos = null!;

        // Act
        var act = async () => await _sut.GetAuditSummaryAsync(repos);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }
}
