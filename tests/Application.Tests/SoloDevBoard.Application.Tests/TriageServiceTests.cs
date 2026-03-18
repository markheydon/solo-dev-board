using Moq;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Triage;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="TriageService"/>.</summary>
public sealed class TriageServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock = new();

    [Fact]
    public void Constructor_GitHubServiceIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IGitHubService? gitHubService = null;

        // Act
        var action = () => _ = new TriageService(gitHubService!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task StartSessionAsync_IssuesOnly_BuildsIssueQueueAndInitialProgress()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Issue { Id = 1, Number = 11, Title = "Older", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
                new Issue { Id = 2, Number = 12, Title = "Newer", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: false);

        // Assert
        Assert.Equal("owner", result.OwnerLogin);
        Assert.Equal("repo", result.RepositoryName);
        Assert.False(result.IncludePullRequests);
        Assert.Equal(2, result.Queue.Count);
        Assert.Equal(TriageItemTypeDto.Issue, result.Queue[0].ItemType);
        Assert.Equal(11, result.Queue[0].Number);
        Assert.Equal(0, result.CurrentIndex);
        Assert.Equal(2, result.Progress.TotalItems);
        Assert.Equal(0, result.Progress.ProcessedItems);
        Assert.Equal(2, result.Progress.RemainingItems);
        _gitHubServiceMock.Verify(service => service.GetPullRequestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartSessionAsync_IncludePullRequests_CombinesIssuesAndPullRequests()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Issue { Id = 1, Number = 11, Title = "Issue", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            ]);

        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PullRequest { Id = 2, Number = 21, Title = "Pull request", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: true);

        // Assert
        Assert.Equal(2, result.Queue.Count);
        Assert.Contains(result.Queue, item => item.ItemType == TriageItemTypeDto.Issue);
        Assert.Contains(result.Queue, item => item.ItemType == TriageItemTypeDto.PullRequest);
    }

    [Fact]
    public async Task AdvanceSessionAsync_QueueHasItems_IncrementsCurrentIndex()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 2, currentIndex: 0);

        // Act
        var result = await sut.AdvanceSessionAsync(session);

        // Assert
        Assert.Equal(1, result.CurrentIndex);
        Assert.Equal(1, result.Progress.ProcessedItems);
        Assert.Equal(1, result.Progress.RemainingItems);
    }

    [Fact]
    public async Task SkipCurrentItemAsync_ActiveItemExists_AddsSkippedItemAndMovesForward()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 2, currentIndex: 0);

        // Act
        var result = await sut.SkipCurrentItemAsync(session, "Needs follow-up");

        // Assert
        Assert.Equal(1, result.CurrentIndex);
        Assert.Single(result.SkippedItems);
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.Skipped, result.ActionHistory[0].ActionType);
        Assert.Contains("Needs follow-up", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Progress.SkippedItems);
    }

    [Fact]
    public async Task RevisitSkippedItemsAsync_SkippedItemsExist_ClearsSkippedList()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var queue = new[]
        {
            new TriageItemDto(TriageItemTypeDto.Issue, 1, 11, "owner/repo", "Item 1", string.Empty, string.Empty, "open", "mark", [], null, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        };
        var skipped = new[]
        {
            new TriageItemDto(TriageItemTypeDto.Issue, 1, 11, "owner/repo", "Item 1", string.Empty, string.Empty, "open", "mark", [], null, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        };

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            queue,
            1,
            skipped,
            [],
            new TriageSessionProgressDto(1, 1, 0, 1),
            new TriageSessionSummaryDto(1, 1, 0, 1, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var result = await sut.RevisitSkippedItemsAsync(session);

        // Assert
        Assert.Empty(result.SkippedItems);
        Assert.Equal(0, result.Progress.SkippedItems);
    }

    [Fact]
    public void BuildSessionSummary_ActionHistoryIncludesAllActionTypes_ReturnsComputedCounts()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var actions = new[]
        {
            new TriageActionDto(TriageActionTypeDto.LabelApplied, TriageItemTypeDto.Issue, 1, "owner/repo", string.Empty, DateTimeOffset.UtcNow),
            new TriageActionDto(TriageActionTypeDto.LabelApplied, TriageItemTypeDto.Issue, 2, "owner/repo", string.Empty, DateTimeOffset.UtcNow),
            new TriageActionDto(TriageActionTypeDto.MilestoneAssigned, TriageItemTypeDto.Issue, 3, "owner/repo", string.Empty, DateTimeOffset.UtcNow),
            new TriageActionDto(TriageActionTypeDto.ProjectBoardAssigned, TriageItemTypeDto.Issue, 4, "owner/repo", string.Empty, DateTimeOffset.UtcNow),
            new TriageActionDto(TriageActionTypeDto.ClosedAsDuplicate, TriageItemTypeDto.Issue, 5, "owner/repo", string.Empty, DateTimeOffset.UtcNow),
        };

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            CreateQueue(3),
            2,
            [],
            actions,
            new TriageSessionProgressDto(3, 2, 1, 0),
            new TriageSessionSummaryDto(3, 2, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var result = sut.BuildSessionSummary(session);

        // Assert
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.ProcessedItems);
        Assert.Equal(1, result.RemainingItems);
        Assert.Equal(2, result.LabelsAppliedCount);
        Assert.Equal(1, result.MilestonesAssignedCount);
        Assert.Equal(1, result.ProjectAssignmentsCount);
        Assert.Equal(1, result.DuplicateClosuresCount);
    }

    private static TriageSessionDto CreateSession(int queueCount, int currentIndex)
    {
        var queue = CreateQueue(queueCount);
        return new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            queue,
            currentIndex,
            [],
            [],
            new TriageSessionProgressDto(queueCount, currentIndex, Math.Max(queueCount - currentIndex, 0), 0),
            new TriageSessionSummaryDto(queueCount, currentIndex, Math.Max(queueCount - currentIndex, 0), 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<TriageItemDto> CreateQueue(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new TriageItemDto(
                TriageItemTypeDto.Issue,
                index,
                index,
                "owner/repo",
                $"Item {index}",
                string.Empty,
                string.Empty,
                "open",
                "mark",
                [],
                null,
                string.Empty,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow))
            .ToArray();
    }
}
