using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusRecommendationMapper"/> ranking fixtures.</summary>
public sealed class DailyFocusRecommendationMapperTests
{
    [Fact]
    public void SelectTopThree_PriorityThenRecency_ReturnsHighestThree()
    {
        var older = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var newer = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        var newest = DateTimeOffset.Parse("2026-08-18T00:00:00Z");

        var workItems = new[]
        {
            CreateWorkItem("owner/a", 1, "Low old", ["priority/low"], older),
            CreateWorkItem("owner/a", 2, "Medium newer", ["priority/medium"], newer),
            CreateWorkItem("owner/a", 3, "High oldest", ["priority/high"], older),
            CreateWorkItem("owner/b", 4, "Critical", ["priority/critical"], older),
            CreateWorkItem("owner/b", 5, "Unlabelled newest", [], newest),
            CreateWorkItem("owner/b", 6, "Medium oldest", ["priority/medium"], older),
        };

        var result = DailyFocusRecommendationMapper.SelectTopThree(workItems, []);

        Assert.Equal(3, result.Count);
        Assert.Equal(4, result[0].Number);
        Assert.Equal("priority/critical", result[0].PriorityLabel);
        Assert.Equal(3, result[1].Number);
        Assert.Equal("priority/high", result[1].PriorityLabel);
        Assert.Equal(2, result[2].Number);
        Assert.Equal("priority/medium", result[2].PriorityLabel);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(2, result[1].Rank);
        Assert.Equal(3, result[2].Rank);
    }

    [Fact]
    public void SelectTopThree_SamePriority_OrdersByUpdatedAtDescending()
    {
        var older = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var newer = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        var newest = DateTimeOffset.Parse("2026-08-18T00:00:00Z");

        var workItems = new[]
        {
            CreateWorkItem("owner/a", 1, "Older high", ["priority/high"], older),
            CreateWorkItem("owner/a", 2, "Newest high", ["priority/high"], newest),
            CreateWorkItem("owner/a", 3, "Newer high", ["priority/high"], newer),
        };

        var result = DailyFocusRecommendationMapper.SelectTopThree(workItems, []);

        Assert.Equal([2, 3, 1], result.Select(item => item.Number).ToArray());
    }

    [Fact]
    public void SelectTopThree_BlockedLabelAndParkedBoardStatuses_AreExcluded()
    {
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            CreateWorkItem("owner/a", 1, "Blocked label", ["priority/critical", "status/blocked"], updated),
            CreateWorkItem("owner/a", 2, "Ice box label", ["priority/critical", "status/ice-box"], updated),
            CreateWorkItem("owner/a", 10, "Board blocked", ["priority/high"], updated),
            CreateWorkItem("owner/a", 11, "Board ice box", ["priority/high"], updated),
            CreateWorkItem("owner/a", 12, "In progress", ["priority/high"], updated),
            CreateWorkItem("owner/a", 20, "Eligible", ["priority/medium"], updated),
        };

        var boardItems = new[]
        {
            CreateBoardItem(10, "Blocked", ProjectBoardItemContentTypeDto.Issue),
            CreateBoardItem(11, "Ice Box", ProjectBoardItemContentTypeDto.Issue),
            CreateBoardItem(12, "In Progress", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = DailyFocusRecommendationMapper.SelectTopThree(workItems, boardItems);

        var recommended = Assert.Single(result);
        Assert.Equal(20, recommended.Number);
        Assert.Equal("owner/a#20", $"{recommended.RepositoryFullName}#{recommended.Number}");
        Assert.Equal("https://github.com/owner/a/issues/20", recommended.HtmlUrl);
    }

    [Fact]
    public void SelectTopThree_UpNextItem_RemainsEligible()
    {
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            CreateWorkItem("owner/a", 7, "Queued", ["priority/high"], updated),
        };
        var boardItems = new[]
        {
            CreateBoardItem(7, "Up Next", ProjectBoardItemContentTypeDto.Issue),
        };

        var result = DailyFocusRecommendationMapper.SelectTopThree(workItems, boardItems);

        var recommended = Assert.Single(result);
        Assert.Equal(7, recommended.Number);
    }

    [Fact]
    public void SelectTopThree_IssueAndPullRequestShareNumber_ExcludesOnlyMatchingType()
    {
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            CreateWorkItem("owner/a", 8, "Issue still open", ["priority/high"], updated),
            CreateWorkItem(
                "owner/a",
                8,
                "PR in progress",
                ["priority/critical"],
                updated,
                PmWorkItemTypeDto.PullRequest),
        };
        var boardItems = new[]
        {
            CreateBoardItem(8, "In Progress", ProjectBoardItemContentTypeDto.PullRequest),
        };

        var result = DailyFocusRecommendationMapper.SelectTopThree(workItems, boardItems);

        var recommended = Assert.Single(result);
        Assert.Equal("Issue still open", recommended.Title);
    }

    [Fact]
    public void SelectTopThree_FewerThanThreeEligible_ReturnsAllEligible()
    {
        var result = DailyFocusRecommendationMapper.SelectTopThree([], []);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectTopThree_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusRecommendationMapper.SelectTopThree(null!, []));
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusRecommendationMapper.SelectTopThree([], null!));
    }

    [Theory]
    [InlineData("Blocked", true)]
    [InlineData("blocked", true)]
    [InlineData("Ice Box", true)]
    [InlineData("In Progress", true)]
    [InlineData("Up Next", false)]
    [InlineData("Todo", false)]
    [InlineData(null, false)]
    public void IsExcludedBoardStatus_StatusName_ExpectedOutcome(string? statusName, bool expected)
    {
        Assert.Equal(expected, DailyFocusRecommendationMapper.IsExcludedBoardStatus(statusName));
    }

    private static PmWorkItemDto CreateWorkItem(
        string repositoryFullName,
        int number,
        string title,
        IReadOnlyList<string> labels,
        DateTimeOffset updatedAt,
        PmWorkItemTypeDto itemType = PmWorkItemTypeDto.Issue)
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
            CreatedAt: updatedAt,
            UpdatedAt: updatedAt,
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
            DateTimeOffset.UnixEpoch);
}
