using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BacklogReviewGrouping"/> membership predicates.</summary>
public sealed class BacklogReviewGroupingTests
{
    [Fact]
    public void Group_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group(null!, [], []));
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group([], null!, []));
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group([], [], null!));
    }

    [Fact]
    public void IsUrgent_HighAndCritical_ReturnsTrue()
    {
        var high = CreateWorkItem(1, "Urgent high", ["priority/high"]);
        var critical = CreateWorkItem(2, "Urgent critical", ["priority/critical"]);
        var medium = CreateWorkItem(3, "Medium", ["priority/medium"]);

        Assert.True(BacklogReviewGrouping.IsUrgent(high));
        Assert.True(BacklogReviewGrouping.IsUrgent(critical));
        Assert.False(BacklogReviewGrouping.IsUrgent(medium));
    }

    [Fact]
    public void IsReadyToStart_UnblockedAndNotCommitted_ReturnsTrue()
    {
        var item = CreateWorkItem(10, "Ready story", ["type/story", "priority/medium"]);

        Assert.True(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsReadyToStart(item, "Todo"));
    }

    [Theory]
    [InlineData("Up Next")]
    [InlineData("In Progress")]
    [InlineData("Blocked")]
    [InlineData("Ice Box")]
    public void IsReadyToStart_CommittedOrParkedBoardStatus_ReturnsFalse(string boardStatus)
    {
        var item = CreateWorkItem(11, "Not ready", ["type/story", "priority/high"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatus));
    }

    [Fact]
    public void IsReadyToStart_BlockedLabel_ReturnsFalse()
    {
        var item = CreateWorkItem(12, "Blocked", ["type/story", "priority/medium", "status/blocked"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
    }

    [Fact]
    public void IsBlockedOrDeferred_StatusLabelsAndParkedBoard_ReturnsTrue()
    {
        var blocked = CreateWorkItem(20, "Label blocked", ["status/blocked"]);
        var iceBoxed = CreateWorkItem(21, "Label ice box", ["status/ice-box"]);
        var unlabelled = CreateWorkItem(22, "Board parked", ["priority/low"]);

        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(blocked, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(iceBoxed, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Blocked"));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Ice Box"));
        Assert.False(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Todo"));
    }

    [Fact]
    public void Group_OverlappingUrgentAndReady_PlacesItemInBothLists()
    {
        var item = CreateWorkItem(30, "Urgent ready", ["priority/critical", "type/bug"]);

        var result = BacklogReviewGrouping.Group([item], [], []);

        Assert.Equal(30, Assert.Single(result.Urgent).Number);
        Assert.Equal(30, Assert.Single(result.ReadyToStart).Number);
        Assert.Empty(result.BlockedOrDeferred);
    }

    [Fact]
    public void Group_UrgentOnUpNext_IsUrgentButNotReady()
    {
        var item = CreateWorkItem(31, "Queued urgent", ["priority/high", "type/story"]);
        var boardItems = new[]
        {
            CreateBoardItem(31, "Up Next", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = BacklogReviewGrouping.Group([item], boardItems, []);

        Assert.Equal(31, Assert.Single(result.Urgent).Number);
        Assert.Empty(result.ReadyToStart);
        Assert.Empty(result.BlockedOrDeferred);
    }

    [Fact]
    public void Group_BoardIceBoxWithoutLabel_IsBlockedNotReady()
    {
        var item = CreateWorkItem(32, "Parked", ["priority/medium", "type/story"]);
        var boardItems = new[]
        {
            CreateBoardItem(32, "Ice Box", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = BacklogReviewGrouping.Group([item], boardItems, []);

        Assert.Empty(result.Urgent);
        Assert.Empty(result.ReadyToStart);
        var blocked = Assert.Single(result.BlockedOrDeferred);
        Assert.Equal(32, blocked.Number);
        Assert.Equal("Ice Box", blocked.BoardStatusName);
    }

    [Fact]
    public void Group_IssueAndPullRequestShareNumber_JoinsOnlyMatchingType()
    {
        var issue = CreateWorkItem(8, "Issue still open", ["priority/medium", "type/story"]);
        var pullRequest = CreateWorkItem(
            8,
            "PR parked",
            ["priority/high"],
            PmWorkItemTypeDto.PullRequest);
        var boardItems = new[]
        {
            CreateBoardItem(8, "Blocked", ProjectBoardItemContentTypeDto.PullRequest),
        };

        var result = BacklogReviewGrouping.Group([issue, pullRequest], boardItems, []);

        Assert.Equal(8, Assert.Single(result.Urgent).Number);
        Assert.Equal("PR parked", result.Urgent[0].Title);
        Assert.Equal("Issue still open", Assert.Single(result.ReadyToStart).Title);
        Assert.Equal("PR parked", Assert.Single(result.BlockedOrDeferred).Title);
    }

    [Fact]
    public void Group_SortsByPriorityThenRepositoryThenNumber()
    {
        var items = new[]
        {
            CreateWorkItem(2, "Second medium", ["priority/medium"], repositoryFullName: "owner/b"),
            CreateWorkItem(1, "First medium", ["priority/medium"], repositoryFullName: "owner/a"),
            CreateWorkItem(9, "Critical", ["priority/critical"], repositoryFullName: "owner/z"),
        };

        var result = BacklogReviewGrouping.Group(items, [], []);

        Assert.Equal([9, 1, 2], result.ReadyToStart.Select(item => item.Number).ToArray());
    }

    [Fact]
    public void Group_DuplicateBoardJoinKeys_KeepsFirstStatus()
    {
        var item = CreateWorkItem(50, "Duplicate key item", ["priority/medium", "type/story"]);
        var boardItems = new[]
        {
            CreateBoardItem(50, "Todo", ProjectBoardItemContentTypeDto.Issue),
            CreateBoardItem(50, "Blocked", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = BacklogReviewGrouping.Group([item], boardItems, []);

        Assert.Equal(50, Assert.Single(result.ReadyToStart).Number);
        Assert.Empty(result.BlockedOrDeferred);
    }

    [Fact]
    public void Group_CarriesFailuresThrough()
    {
        var failures = new[]
        {
            new PmRepositoryCatalogueFailureDto("owner/failed", "Not found", 404),
        };

        var result = BacklogReviewGrouping.Group([], [], failures);

        Assert.Same(failures, result.Failures);
    }

    private static PmWorkItemDto CreateWorkItem(
        int number,
        string title,
        IReadOnlyList<string> labels,
        PmWorkItemTypeDto itemType = PmWorkItemTypeDto.Issue,
        string repositoryFullName = "owner/a")
    {
        var path = itemType == PmWorkItemTypeDto.PullRequest ? "pull" : "issues";
        return new PmWorkItemDto(
            itemType,
            number,
            title,
            $"https://github.com/{repositoryFullName}/{path}/{number}",
            repositoryFullName,
            labels,
            MilestoneNumber: null,
            MilestoneTitle: null,
            CreatedAt: DateTimeOffset.Parse("2026-08-18T00:00:00Z"),
            UpdatedAt: DateTimeOffset.Parse("2026-08-18T00:00:00Z"),
            IsDraft: itemType == PmWorkItemTypeDto.PullRequest ? false : null,
            HasReviewPending: itemType == PmWorkItemTypeDto.PullRequest ? false : null,
            SubIssueTotal: null,
            SubIssueCompleted: null);
    }

    private static ProjectBoardItemDto CreateBoardItem(
        int number,
        string statusName,
        ProjectBoardItemContentTypeDto contentType)
        => new(
            $"PVTI_{number}",
            new ProjectBoardItemStatusDto($"opt-{number}", statusName),
            FocusOrder: null,
            new ProjectBoardItemContentDto(
                contentType,
                number,
                "owner",
                "a",
                "Board title",
                "https://github.com/owner/a/issues/1"),
            DateTimeOffset.UnixEpoch,
            false);
}
