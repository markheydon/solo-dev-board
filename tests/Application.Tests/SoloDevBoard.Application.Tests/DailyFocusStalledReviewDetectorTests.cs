using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusStalledReviewDetector"/>.</summary>
public sealed class DailyFocusStalledReviewDetectorTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("In Review", true)]
    [InlineData("in review", true)]
    [InlineData("Waiting on review", true)]
    [InlineData("Code Review", true)]
    [InlineData("Awaiting Review", true)]
    [InlineData("Todo", false)]
    [InlineData("In Progress", false)]
    [InlineData("Preview", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsInReviewEquivalent_StatusName_ExpectedOutcome(string? statusName, bool expected)
    {
        Assert.Equal(expected, DailyFocusStalledReviewDetector.IsInReviewEquivalent(statusName));
    }

    [Fact]
    public void BoardHasInReviewStatus_DiscoveredInReviewOption_ReturnsTrue()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
            new ProjectBoardStatusOptionDto("opt-review", "In Review"),
        };

        var result = DailyFocusStalledReviewDetector.BoardHasInReviewStatus(statusOptions, []);

        Assert.True(result);
    }

    [Fact]
    public void BoardHasInReviewStatus_WaitingOnReviewOption_ReturnsTrue()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-waiting", "Waiting on review"),
        };

        var result = DailyFocusStalledReviewDetector.BoardHasInReviewStatus(statusOptions, []);

        Assert.True(result);
    }

    [Fact]
    public void BoardHasInReviewStatus_NoReviewColumn_ReturnsFalse()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
            new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
        };

        var result = DailyFocusStalledReviewDetector.BoardHasInReviewStatus(statusOptions, []);

        Assert.False(result);
    }

    [Fact]
    public void DetectFromBoardColumn_PullRequestInReviewForThreeDays_IsIncluded()
    {
        var items = new[]
        {
            CreateBoardItem(
                ProjectBoardItemContentTypeDto.PullRequest,
                12,
                "owner",
                "repo",
                "In Review",
                UtcNow.AddDays(-3),
                "https://github.com/owner/repo/pull/12"),
            CreateBoardItem(
                ProjectBoardItemContentTypeDto.PullRequest,
                13,
                "owner",
                "repo",
                "In Review",
                UtcNow.AddDays(-3).AddMinutes(1),
                "https://github.com/owner/repo/pull/13"),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromBoardColumn(items, UtcNow, stallDays: 3, []);

        var stalled = Assert.Single(result);
        Assert.Equal("owner/repo", stalled.RepositoryFullName);
        Assert.Equal(12, stalled.Number);
        Assert.Equal(3, stalled.AgeDays);
        Assert.Equal("https://github.com/owner/repo/pull/12", stalled.HtmlUrl);
    }

    [Fact]
    public void DetectFromBoardColumn_IssueInReviewColumn_IsIgnored()
    {
        var items = new[]
        {
            CreateBoardItem(
                ProjectBoardItemContentTypeDto.Issue,
                40,
                "owner",
                "repo",
                "In Review",
                UtcNow.AddDays(-5),
                "https://github.com/owner/repo/issues/40"),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromBoardColumn(items, UtcNow, stallDays: 3, []);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectFromBoardColumn_ExcludedRepository_IsOmitted()
    {
        var items = new[]
        {
            CreateBoardItem(
                ProjectBoardItemContentTypeDto.PullRequest,
                12,
                "owner",
                "skipped",
                "In Review",
                UtcNow.AddDays(-5),
                "https://github.com/owner/skipped/pull/12"),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromBoardColumn(
            items,
            UtcNow,
            stallDays: 3,
            ["owner/skipped"]);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectFromPendingReviewCatalogue_OpenNonDraftPendingReviewAgedThreeDays_IsIncluded()
    {
        var items = new[]
        {
            CreateWorkItem(
                number: 12,
                createdAt: UtcNow.AddDays(-3),
                isDraft: false,
                hasReviewPending: true),
            CreateWorkItem(
                number: 13,
                createdAt: UtcNow.AddDays(-2),
                isDraft: false,
                hasReviewPending: true),
            CreateWorkItem(
                number: 14,
                createdAt: UtcNow.AddDays(-10),
                isDraft: true,
                hasReviewPending: true),
            CreateWorkItem(
                number: 15,
                createdAt: UtcNow.AddDays(-10),
                isDraft: false,
                hasReviewPending: false),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue(items, UtcNow, stallDays: 3, []);

        var stalled = Assert.Single(result);
        Assert.Equal("owner/repo", stalled.RepositoryFullName);
        Assert.Equal(12, stalled.Number);
        Assert.Equal(3, stalled.AgeDays);
        Assert.Equal("https://github.com/owner/repo/pull/12", stalled.HtmlUrl);
    }

    [Fact]
    public void DetectFromPendingReviewCatalogue_IssueItems_AreIgnored()
    {
        var items = new[]
        {
            new PlanningWorkItemDto(
                PlanningWorkItemTypeDto.Issue,
                1,
                "Issue",
                "https://github.com/owner/repo/issues/1",
                "owner/repo",
                [],
                null,
                null,
                UtcNow.AddDays(-10),
                UtcNow,
                IsDraft: null,
                HasReviewPending: null,
                SubIssueTotal: null,
                SubIssueCompleted: null),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue(items, UtcNow, stallDays: 3, []);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectFromPendingReviewCatalogue_ExcludedRepository_IsOmitted()
    {
        var items = new[]
        {
            CreateWorkItem(
                number: 12,
                createdAt: UtcNow.AddDays(-5),
                isDraft: false,
                hasReviewPending: true,
                repositoryFullName: "owner/skipped"),
            CreateWorkItem(
                number: 13,
                createdAt: UtcNow.AddDays(-5),
                isDraft: false,
                hasReviewPending: true,
                repositoryFullName: "owner/kept"),
        };

        var result = DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue(
            items,
            UtcNow,
            stallDays: 3,
            ["owner/skipped"]);

        var stalled = Assert.Single(result);
        Assert.Equal("owner/kept", stalled.RepositoryFullName);
        Assert.Equal(13, stalled.Number);
    }

    [Fact]
    public void DetectFromBoardColumn_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusStalledReviewDetector.DetectFromBoardColumn(null!, UtcNow, 3, []));
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusStalledReviewDetector.DetectFromBoardColumn([], UtcNow, 3, null!));
    }

    [Fact]
    public void DetectFromPendingReviewCatalogue_NullWorkItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue(null!, UtcNow, 3, []));
        Assert.Throws<ArgumentNullException>(() =>
            DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue([], UtcNow, 3, null!));
    }

    private static ProjectBoardItemDto CreateBoardItem(
        ProjectBoardItemContentTypeDto contentType,
        int number,
        string owner,
        string repo,
        string statusName,
        DateTimeOffset activityTimestamp,
        string url)
        => new(
            "PVTI_item",
            new ProjectBoardItemStatusDto("opt-review", statusName),
            FocusOrder: null,
            new ProjectBoardItemContentDto(contentType, number, owner, repo, $"PR {number}", url),
            activityTimestamp,
            false);

    private static PlanningWorkItemDto CreateWorkItem(
        int number,
        DateTimeOffset createdAt,
        bool isDraft,
        bool hasReviewPending,
        string repositoryFullName = "owner/repo")
        => new(
            PlanningWorkItemTypeDto.PullRequest,
            number,
            $"PR {number}",
            $"https://github.com/{repositoryFullName}/pull/{number}",
            repositoryFullName,
            [],
            null,
            null,
            createdAt,
            createdAt,
            isDraft,
            hasReviewPending,
            SubIssueTotal: null,
            SubIssueCompleted: null);
}
