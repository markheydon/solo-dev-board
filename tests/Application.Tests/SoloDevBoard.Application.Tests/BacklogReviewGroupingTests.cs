using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BacklogReviewGrouping"/> membership predicates.</summary>
public sealed class BacklogReviewGroupingTests
{
    private static readonly DateTimeOffset ReferenceTime = DateTimeOffset.Parse("2026-08-20T12:00:00Z");

    [Fact]
    public void Group_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group(null!, [], [], [], 14, ReferenceTime));
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group([], null!, [], [], 14, ReferenceTime));
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group([], [], null!, [], 14, ReferenceTime));
        Assert.Throws<ArgumentNullException>(() =>
            BacklogReviewGrouping.Group([], [], [], null!, 14, ReferenceTime));
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

    [Theory]
    [InlineData(new[] { "type/story" }, true)]
    [InlineData(new[] { "priority/high" }, true)]
    [InlineData(new[] { "type/story", "priority/high" }, false)]
    public void IsAwaitingTriage_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        var item = CreateWorkItem(4, "Triage candidate", labels);

        Assert.Equal(expected, BacklogReviewGrouping.IsAwaitingTriage(item));
    }

    [Fact]
    public void IsReadyToStart_UnblockedAndNotCommitted_ReturnsTrue()
    {
        var item = CreateWorkItem(10, "Ready story", ["type/story", "priority/medium"]);

        Assert.True(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsReadyToStart(item, "Todo"));
    }

    [Fact]
    public void IsReadyToStart_Urgent_ReturnsFalse()
    {
        var item = CreateWorkItem(11, "Urgent ready", ["priority/critical", "type/bug"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
    }

    [Fact]
    public void IsReadyToStart_AwaitingTriage_ReturnsFalse()
    {
        var item = CreateWorkItem(12, "Missing priority", ["type/story"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
    }

    [Theory]
    [InlineData("Up Next")]
    [InlineData("In Progress")]
    [InlineData("Blocked")]
    [InlineData("Ice Box")]
    public void IsReadyToStart_CommittedOrParkedBoardStatus_ReturnsFalse(string boardStatus)
    {
        var item = CreateWorkItem(13, "Not ready", ["type/story", "priority/medium"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatus));
    }

    [Fact]
    public void IsReadyToStart_BlockedLabel_ReturnsFalse()
    {
        var item = CreateWorkItem(14, "Blocked", ["type/story", "priority/medium", "status/blocked"]);

        Assert.False(BacklogReviewGrouping.IsReadyToStart(item, boardStatusName: null));
    }

    [Fact]
    public void IsBlockedOrDeferred_StatusLabelsAndParkedBoard_ReturnsTrue()
    {
        var blocked = CreateWorkItem(20, "Label blocked", ["status/blocked"]);
        var iceBoxed = CreateWorkItem(21, "Label ice box", ["status/ice-box"]);
        var unlabelled = CreateWorkItem(22, "Board parked", ["priority/low", "type/story"]);

        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(blocked, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(iceBoxed, boardStatusName: null));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Blocked"));
        Assert.True(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Ice Box"));
        Assert.False(BacklogReviewGrouping.IsBlockedOrDeferred(unlabelled, "Todo"));
    }

    [Fact]
    public void Group_UrgentReadyItem_IsUrgentOnlyNotReady()
    {
        var item = CreateWorkItem(30, "Urgent ready", ["priority/critical", "type/bug"]);

        var result = BacklogReviewGrouping.Group([item], [], [], [], 14, ReferenceTime);

        Assert.Equal(30, Assert.Single(result.Urgent).Number);
        Assert.Empty(result.ReadyToStart);
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

        var result = BacklogReviewGrouping.Group([item], boardItems, [], [], 14, ReferenceTime);

        Assert.Equal(31, Assert.Single(result.Urgent).Number);
        Assert.Empty(result.ReadyToStart);
        Assert.Empty(result.BlockedOrDeferred);
    }

    [Fact]
    public void Group_MissingCoreLabels_GoesToAwaitingTriageNotReady()
    {
        var item = CreateWorkItem(32, "Needs labels", ["type/story"]);

        var result = BacklogReviewGrouping.Group([item], [], [], [], 14, ReferenceTime);

        Assert.Equal(32, Assert.Single(result.AwaitingTriage).Number);
        Assert.Empty(result.ReadyToStart);
    }

    [Fact]
    public void Group_BoardIceBoxWithoutLabel_IsBlockedNotReady()
    {
        var item = CreateWorkItem(33, "Parked", ["priority/medium", "type/story"]);
        var boardItems = new[]
        {
            CreateBoardItem(33, "Ice Box", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = BacklogReviewGrouping.Group([item], boardItems, [], [], 14, ReferenceTime);

        Assert.Empty(result.Urgent);
        Assert.Empty(result.ReadyToStart);
        var blocked = Assert.Single(result.BlockedOrDeferred);
        Assert.Equal(33, blocked.Number);
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

        var result = BacklogReviewGrouping.Group([issue, pullRequest], boardItems, [], [], 14, ReferenceTime);

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
            CreateWorkItem(2, "Second medium", ["priority/medium", "type/story"], repositoryFullName: "owner/b"),
            CreateWorkItem(1, "First medium", ["priority/medium", "type/story"], repositoryFullName: "owner/a"),
            CreateWorkItem(9, "Critical", ["priority/critical", "type/story"], repositoryFullName: "owner/z"),
        };

        var result = BacklogReviewGrouping.Group(items, [], [], [], 14, ReferenceTime);

        Assert.Equal([9], result.Urgent.Select(item => item.Number).ToArray());
        Assert.Equal([1, 2], result.ReadyToStart.Select(item => item.Number).ToArray());
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

        var result = BacklogReviewGrouping.Group([item], boardItems, [], [], 14, ReferenceTime);

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

        var result = BacklogReviewGrouping.Group([], [], [], failures, 14, ReferenceTime);

        Assert.Same(failures, result.Failures);
    }

    [Theory]
    [InlineData(13, false)]
    [InlineData(14, true)]
    [InlineData(15, true)]
    public void IsNeglected_BoundaryDays_ReturnsExpected(int daysSinceActivity, bool expected)
    {
        var summary = new PmRepositorySummaryDto(
            "owner/quiet",
            0,
            0,
            ReferenceTime.AddDays(-daysSinceActivity),
            IsIncluded: true);

        Assert.Equal(expected, BacklogReviewGrouping.IsNeglected(summary, neglectDays: 14, ReferenceTime));
    }

    [Fact]
    public void IsNeglected_NoRecordedActivity_ReturnsTrue()
    {
        var summary = new PmRepositorySummaryDto("owner/empty", 0, 0, default, IsIncluded: true);

        Assert.True(BacklogReviewGrouping.IsNeglected(summary, neglectDays: 14, ReferenceTime));
    }

    [Fact]
    public void Group_NeglectedRepositories_ListsInactiveIncludedRepos()
    {
        var summaries = new[]
        {
            new PmRepositorySummaryDto(
                "owner/active",
                2,
                1,
                ReferenceTime.AddDays(-3),
                IsIncluded: true),
            new PmRepositorySummaryDto(
                "owner/quiet",
                0,
                0,
                ReferenceTime.AddDays(-20),
                IsIncluded: true),
            new PmRepositorySummaryDto(
                "owner/excluded",
                0,
                0,
                ReferenceTime.AddDays(-30),
                IsIncluded: false),
        };

        var result = BacklogReviewGrouping.Group([], [], summaries, [], 14, ReferenceTime);

        var neglected = Assert.Single(result.NeglectedRepositories);
        Assert.Equal("owner/quiet", neglected.FullName);
        Assert.Equal(0, neglected.OpenIssueCount);
        Assert.Equal(0, neglected.OpenPullRequestCount);
        Assert.Equal(ReferenceTime.AddDays(-20), neglected.LastActivityAt);
    }

    [Fact]
    public void Group_EpicNearComplete_IncludesCompletedParent()
    {
        var epic = CreateWorkItem(
            60,
            "Done epic",
            ["type/epic", "priority/high"],
            subIssueTotal: 3,
            subIssueCompleted: 3);

        var result = BacklogReviewGrouping.Group([epic], [], [], [], 14, ReferenceTime);

        var nearComplete = Assert.Single(result.EpicsNearComplete);
        Assert.Equal(60, nearComplete.Number);
        Assert.Equal("type/epic", nearComplete.TypeLabel);
        Assert.Equal(3, nearComplete.SubIssueTotal);
        Assert.False(result.SubIssueCountsUnavailable);
    }

    [Fact]
    public void Group_OpenEpicWithoutSubIssueCounts_SetsUnavailableFlag()
    {
        var epic = CreateWorkItem(61, "Unknown progress", ["type/epic", "priority/medium"]);

        var result = BacklogReviewGrouping.Group([epic], [], [], [], 14, ReferenceTime);

        Assert.Empty(result.EpicsNearComplete);
        Assert.True(result.SubIssueCountsUnavailable);
    }

    private static PmWorkItemDto CreateWorkItem(
        int number,
        string title,
        IReadOnlyList<string> labels,
        PmWorkItemTypeDto itemType = PmWorkItemTypeDto.Issue,
        string repositoryFullName = "owner/a",
        int? subIssueTotal = null,
        int? subIssueCompleted = null)
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
            subIssueTotal,
            subIssueCompleted);
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
