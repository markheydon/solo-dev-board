using Moq;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Triage;
using SoloDevBoard.Domain.Entities.Labels;
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
    public async Task StartSessionAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var action = async () => _ = await sut.StartSessionAsync(" ", "repo");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task StartSessionAsync_RepositoryIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var action = async () => _ = await sut.StartSessionAsync("owner", " ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
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
    public async Task StartSessionAsync_IncludePullRequestsWithLabelledPullRequest_ExcludesLabelledPullRequestFromQueue()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Issue { Id = 1, Number = 11, Title = "Issue", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2), Labels = [] },
            ]);

        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PullRequest
                {
                    Id = 2,
                    Number = 21,
                    Title = "Unlabelled pull request",
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    Labels = [],
                },
                new PullRequest
                {
                    Id = 3,
                    Number = 22,
                    Title = "Labelled pull request",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Labels = [new Label { Name = "type/story" }],
                },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: true);

        // Assert
        Assert.Equal(2, result.Queue.Count);
        Assert.Contains(result.Queue, item => item.ItemType == TriageItemTypeDto.Issue && item.Number == 11);
        Assert.Contains(result.Queue, item => item.ItemType == TriageItemTypeDto.PullRequest && item.Number == 21);
        Assert.DoesNotContain(result.Queue, item => item.ItemType == TriageItemTypeDto.PullRequest && item.Number == 22);
    }

    [Fact]
    public async Task StartSessionAsync_IncludePullRequestsWithMixedItems_OrdersQueueByUpdatedAtAcrossItemTypes()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Issue { Id = 1, Number = 11, Title = "Older issue", UpdatedAt = DateTimeOffset.Parse("2026-03-01T10:00:00Z"), Labels = [] },
                new Issue { Id = 2, Number = 12, Title = "Newer issue", UpdatedAt = DateTimeOffset.Parse("2026-03-03T10:00:00Z"), Labels = [] },
            ]);

        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PullRequest { Id = 3, Number = 21, Title = "Middle pull request", UpdatedAt = DateTimeOffset.Parse("2026-03-02T10:00:00Z"), Labels = [] },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: true);

        // Assert
        Assert.Equal(3, result.Queue.Count);
        Assert.Equal(11, result.Queue[0].Number);
        Assert.Equal(TriageItemTypeDto.Issue, result.Queue[0].ItemType);
        Assert.Equal(21, result.Queue[1].Number);
        Assert.Equal(TriageItemTypeDto.PullRequest, result.Queue[1].ItemType);
        Assert.Equal(12, result.Queue[2].Number);
        Assert.Equal(TriageItemTypeDto.Issue, result.Queue[2].ItemType);
    }

    [Fact]
    public async Task StartSessionAsync_IncludePullRequestsWithMilestoneOnPullRequest_PreservesMilestoneDetails()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _gitHubServiceMock
            .Setup(service => service.GetPullRequestsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PullRequest
                {
                    Id = 3,
                    Number = 21,
                    Title = "Pull request with milestone",
                    UpdatedAt = DateTimeOffset.Parse("2026-03-02T10:00:00Z"),
                    Labels = [],
                    Milestone = new SoloDevBoard.Domain.Entities.Milestones.Milestone
                    {
                        Number = 7,
                        Title = "v0.7.0",
                    },
                },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: true);

        // Assert
        var pullRequest = Assert.Single(result.Queue);
        Assert.Equal(TriageItemTypeDto.PullRequest, pullRequest.ItemType);
        Assert.Equal(7, pullRequest.MilestoneNumber);
        Assert.Equal("v0.7.0", pullRequest.MilestoneTitle);
    }

    [Fact]
    public async Task StartSessionAsync_LabelledIssuePresent_ExcludesLabelledIssueFromQueue()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetIssuesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Issue { Id = 1, Number = 11, Title = "Unlabelled", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2), Labels = [] },
                new Issue { Id = 2, Number = 12, Title = "Labelled", UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1), Labels = [new Label { Name = "priority/high" }] },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);

        // Act
        var result = await sut.StartSessionAsync("owner", "repo", includePullRequests: false);

        // Assert
        Assert.Single(result.Queue);
        Assert.Equal(11, result.Queue[0].Number);
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
        Assert.Equal(0, result.CurrentIndex);
        Assert.Single(result.Queue);
        Assert.Equal(2, result.Queue[0].Number);
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
    public async Task RevisitSkippedItemsAsync_SkippedItemStillPresentInQueue_MovesItemToQueueEnd()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var itemOne = new TriageItemDto(TriageItemTypeDto.Issue, 1, 11, "owner/repo", "Item 1", string.Empty, string.Empty, "open", "mark", [], null, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var itemTwo = new TriageItemDto(TriageItemTypeDto.Issue, 2, 12, "owner/repo", "Item 2", string.Empty, string.Empty, "open", "mark", [], null, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [itemOne, itemTwo],
            1,
            [itemOne],
            [],
            new TriageSessionProgressDto(2, 1, 1, 1),
            new TriageSessionSummaryDto(2, 1, 1, 1, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var result = await sut.RevisitSkippedItemsAsync(session);

        // Assert
        Assert.Equal(2, result.Queue.Count);
        Assert.Equal(12, result.Queue[0].Number);
        Assert.Equal(11, result.Queue[1].Number);
        Assert.Empty(result.SkippedItems);
    }

    [Fact]
    public async Task ApplyLabelToCurrentItemAsync_ActiveItemExists_AppliesLabelAndRecordsAction()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.ApplyLabelToCurrentItemAsync(session, "type/story");

        // Assert
        Assert.Single(result.Queue[0].Labels);
        Assert.Equal("type/story", result.Queue[0].Labels[0]);
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.LabelApplied, result.ActionHistory[0].ActionType);
        Assert.Contains("type/story", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Summary.LabelsAppliedCount);

        _gitHubServiceMock.Verify(
            service => service.ApplyLabelsToTriageItemAsync("owner", "repo", 1, It.Is<IReadOnlyList<string>>(labels => labels.Count == 1 && labels[0] == "type/story"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyLabelToCurrentItemAsync_LabelAlreadyAssigned_DoesNotDuplicateAssignedLabel()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var existingLabelItem = new TriageItemDto(
            TriageItemTypeDto.Issue,
            1,
            99,
            "owner/repo",
            "Item 99",
            string.Empty,
            string.Empty,
            "open",
            "mark",
            ["priority/high"],
            null,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [existingLabelItem],
            0,
            [],
            [],
            new TriageSessionProgressDto(1, 0, 1, 0),
            new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var result = await sut.ApplyLabelToCurrentItemAsync(session, "priority/high");

        // Assert
        Assert.Single(result.Queue[0].Labels);
        Assert.Equal("priority/high", result.Queue[0].Labels[0]);

        _gitHubServiceMock.Verify(
            service => service.ApplyLabelsToTriageItemAsync("owner", "repo", 99, It.Is<IReadOnlyList<string>>(labels => labels.Count == 1 && labels[0] == "priority/high"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyLabelToCurrentItemAsync_InvalidRepositoryScope_ThrowsArgumentException()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var invalidScopeItem = new TriageItemDto(
            TriageItemTypeDto.Issue,
            1,
            1,
            "invalid-scope",
            "Item 1",
            string.Empty,
            string.Empty,
            "open",
            "mark",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [invalidScopeItem],
            0,
            [],
            [],
            new TriageSessionProgressDto(1, 0, 1, 0),
            new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var action = async () => _ = await sut.ApplyLabelToCurrentItemAsync(session, "type/story");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetMilestoneOptionsAsync_MilestonesReturned_ReturnsSortedOptions()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.GetMilestonesAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SoloDevBoard.Domain.Entities.Milestones.Milestone { Number = 3, Title = "v0.3.0" },
                new SoloDevBoard.Domain.Entities.Milestones.Milestone { Number = 1, Title = "v0.1.0" },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.GetMilestoneOptionsAsync(session);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Number);
        Assert.Equal("v0.1.0", result[0].Title);
        Assert.Equal(3, result[1].Number);
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
                    Id = "project-two",
                    Title = "Roadmap",
                    OwnerLogin = "owner",
                    StatusFieldId = "status-two",
                    StatusOptions =
                    [
                        new TriageProjectBoardStatusOption { Id = "done", Name = "Done" },
                        new TriageProjectBoardStatusOption { Id = "in-progress", Name = "In Progress" },
                    ],
                },
                new TriageProjectBoard
                {
                    Id = "project-one",
                    Title = "Backlog",
                    OwnerLogin = "owner",
                    StatusFieldId = "status-one",
                    StatusOptions =
                    [
                        new TriageProjectBoardStatusOption { Id = "todo", Name = "Todo" },
                    ],
                },
            ]);

        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.GetProjectBoardOptionsAsync(session);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Backlog", result[0].Title);
        Assert.Equal("Roadmap", result[1].Title);
        Assert.Equal("Done", result[1].StatusOptions[0].Name);
        Assert.Equal("In Progress", result[1].StatusOptions[1].Name);
    }

    [Fact]
    public async Task AssignMilestoneToCurrentItemAsync_ValidMilestone_AssignsMilestoneAndRecordsAction()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.AssignMilestoneToCurrentItemAsync(session, 12, "v1.2.0");

        // Assert
        Assert.Equal(12, result.Queue[0].MilestoneNumber);
        Assert.Equal("v1.2.0", result.Queue[0].MilestoneTitle);
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.MilestoneAssigned, result.ActionHistory[0].ActionType);
        Assert.Equal(1, result.Summary.MilestonesAssignedCount);

        _gitHubServiceMock.Verify(
            service => service.AssignMilestoneToTriageItemAsync("owner", "repo", 1, 12, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddCurrentItemToProjectBoardAsync_ValidInput_AddsItemAndUpdatesStatusAndRecordsAction()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(service => service.AddTriageItemToProjectBoardAsync("owner", "repo", 1, "project-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync("project-item-id");

        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.AddCurrentItemToProjectBoardAsync(
            session,
            "project-id",
            "Roadmap",
            "status-field-id",
            "in-progress",
            "In Progress");

        // Assert
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.ProjectBoardAssigned, result.ActionHistory[0].ActionType);
        Assert.Contains("Roadmap", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Contains("In Progress", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Summary.ProjectAssignmentsCount);

        _gitHubServiceMock.Verify(
            service => service.AddTriageItemToProjectBoardAsync("owner", "repo", 1, "project-id", It.IsAny<CancellationToken>()),
            Times.Once);
        _gitHubServiceMock.Verify(
            service => service.UpdateProjectBoardItemStatusAsync("project-id", "project-item-id", "status-field-id", "in-progress", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddCurrentItemToProjectBoardAsync_InvalidRepositoryScope_ThrowsArgumentException()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var invalidScopeItem = new TriageItemDto(
            TriageItemTypeDto.Issue,
            1,
            1,
            "invalid-scope",
            "Item 1",
            string.Empty,
            string.Empty,
            "open",
            "mark",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [invalidScopeItem],
            0,
            [],
            [],
            new TriageSessionProgressDto(1, 0, 1, 0),
            new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        var action = async () => _ = await sut.AddCurrentItemToProjectBoardAsync(
            session,
            "project-id",
            "Roadmap",
            "status-field-id",
            "in-progress",
            "In Progress");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task CloseCurrentItemAsDuplicateAsync_ActiveIssueExists_ClosesDuplicateAndRecordsAction()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        // Act
        var result = await sut.CloseCurrentItemAsDuplicateAsync(session, "#123");

        // Assert
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.ClosedAsDuplicate, result.ActionHistory[0].ActionType);
        Assert.Contains("#123", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Summary.DuplicateClosuresCount);

        _gitHubServiceMock.Verify(
            service => service.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.Issue, 1, "#123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CloseCurrentItemAsDuplicateAsync_DuplicateLabelExists_AppliesCanonicalDuplicateLabel()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        _gitHubServiceMock
            .Setup(service => service.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.Issue, 1, "#123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _gitHubServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Label { Name = "duplicate" } });

        _gitHubServiceMock
            .Setup(service => service.ApplyLabelsToTriageItemAsync(
                "owner",
                "repo",
                1,
                It.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "duplicate" })),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await sut.CloseCurrentItemAsDuplicateAsync(session, "#123");

        // Assert
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.ClosedAsDuplicate, result.ActionHistory[0].ActionType);
        Assert.Contains("Applied label 'duplicate'", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Summary.DuplicateClosuresCount);

        _gitHubServiceMock.Verify(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()), Times.Once);
        _gitHubServiceMock.Verify(service => service.ApplyLabelsToTriageItemAsync(
            "owner",
            "repo",
            1,
            It.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "duplicate" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseCurrentItemAsDuplicateAsync_DuplicateLabelMissing_DoesNotApplyLabel()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var session = CreateSession(queueCount: 1, currentIndex: 0);

        _gitHubServiceMock
            .Setup(service => service.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.Issue, 1, "#123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _gitHubServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Label>());

        // Act
        var result = await sut.CloseCurrentItemAsDuplicateAsync(session, "#123");

        // Assert
        Assert.Single(result.ActionHistory);
        Assert.Equal(TriageActionTypeDto.ClosedAsDuplicate, result.ActionHistory[0].ActionType);
        Assert.Contains("No canonical duplicate label was available", result.ActionHistory[0].Detail, StringComparison.Ordinal);
        Assert.Equal(1, result.Summary.DuplicateClosuresCount);

        _gitHubServiceMock.Verify(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()), Times.Once);
        _gitHubServiceMock.Verify(service => service.ApplyLabelsToTriageItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseCurrentItemAsDuplicateAsync_ActivePullRequestExists_ClosesPullRequestDuplicate()
    {
        // Arrange
        var sut = new TriageService(_gitHubServiceMock.Object);
        var pullRequestItem = new TriageItemDto(
            TriageItemTypeDto.PullRequest,
            21,
            21,
            "owner/repo",
            "Pull request 21",
            "https://github.com/owner/repo/pull/21",
            string.Empty,
            "open",
            "mark",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [pullRequestItem],
            0,
            [],
            [],
            new TriageSessionProgressDto(1, 0, 1, 0),
            new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        // Act
        _ = await sut.CloseCurrentItemAsDuplicateAsync(session, "https://github.com/owner/repo/pull/20");

        // Assert
        _gitHubServiceMock.Verify(
            service => service.CloseTriageItemAsDuplicateAsync(
                "owner",
                "repo",
                GitHubTriageItemType.PullRequest,
                21,
                "https://github.com/owner/repo/pull/20",
                It.IsAny<CancellationToken>()),
            Times.Once);
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
            new TriageActionDto(TriageActionTypeDto.Skipped, TriageItemTypeDto.Issue, 99, "owner/repo", "Skipped for later review. Reason: Needs wider context", DateTimeOffset.UtcNow),
        };

        var session = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            CreateQueue(3),
            2,
            [
                new TriageItemDto(
                    TriageItemTypeDto.Issue,
                    99,
                    99,
                    "owner/repo",
                    "Skipped item",
                    string.Empty,
                    string.Empty,
                    "open",
                    "mark",
                    [],
                    null,
                    string.Empty,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow),
            ],
            actions,
            new TriageSessionProgressDto(3, 2, 1, 1),
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
        Assert.Equal(2, result.LabelActionDetails.Count);
        Assert.Single(result.MilestoneActionDetails);
        Assert.Single(result.ProjectActionDetails);
        Assert.Single(result.DuplicateActionDetails);
        Assert.Single(result.SkippedActionDetails);
        Assert.Single(result.SkippedItemDetails);
        Assert.Contains("Issue #1", result.LabelActionDetails[0], StringComparison.Ordinal);
        Assert.Contains("Reason: Needs wider context", result.SkippedActionDetails[0], StringComparison.Ordinal);
        Assert.Contains("Skipped item", result.SkippedItemDetails[0], StringComparison.Ordinal);
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
