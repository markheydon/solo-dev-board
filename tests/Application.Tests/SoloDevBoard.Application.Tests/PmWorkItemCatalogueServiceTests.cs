using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.PmWorkflow;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PmWorkItemCatalogueService"/>.</summary>
public sealed class PmWorkItemCatalogueServiceTests
{
    private const string OpenItemState = "open";

    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();
    private readonly IPmSettingsService _pmSettingsService = Substitute.For<IPmSettingsService>();
    private readonly PmWorkItemCatalogueService _sut;

    public PmWorkItemCatalogueServiceTests()
    {
        _pmSettingsService.GetSettingsAsync().Returns(PmSettingsDefaults.Create());
        _sut = new PmWorkItemCatalogueService(
            _gitHubService,
            _pmSettingsService,
            NullLogger<PmWorkItemCatalogueService>.Instance);
    }

    [Fact]
    public async Task GetCatalogueAsync_ActiveRepositoriesExist_ReturnsMappedOpenItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "repo-a", FullName = "owner/repo-a" },
            new Repository { Id = 2, Name = "repo-b", FullName = "owner/repo-b" },
        ]);

        _gitHubService
            .GetIssuesAsync("owner", "repo-a", OpenItemState, cancellationToken)
            .Returns(
            [
                CreateIssue(10, "Epic parent", ["type/epic", "priority/high"], milestoneTitle: "v1.1.0"),
                CreateIssue(11, "Closed issue", ["type/story", "priority/medium"], state: "closed"),
            ]);
        _gitHubService
            .GetPullRequestsAsync("owner", "repo-a", OpenItemState, cancellationToken)
            .Returns(
            [
                CreatePullRequest(20, "Review me", ["type/story", "priority/medium"], isDraft: false),
            ]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "repo-a", cancellationToken)
            .Returns([new PullRequestReviewMetadata { Number = 20, HasReviewPending = true }]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "repo-a", Arg.Is<IReadOnlyList<int>>(numbers => numbers.SequenceEqual(new[] { 10 })), cancellationToken)
            .Returns([new IssueSubIssueSummary { Number = 10, TotalCount = 2, CompletedCount = 1 }]);

        _gitHubService
            .GetIssuesAsync("owner", "repo-b", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetPullRequestsAsync("owner", "repo-b", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "repo-b", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "repo-b", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Items.Count);

        var issue = Assert.Single(result.Items, item => item.ItemType == PmWorkItemTypeDto.Issue);
        Assert.Equal(10, issue.Number);
        Assert.Equal("Epic parent", issue.Title);
        Assert.Equal("owner/repo-a", issue.RepositoryFullName);
        Assert.Equal("v1.1.0", issue.MilestoneTitle);
        Assert.Null(issue.IsDraft);
        Assert.Null(issue.HasReviewPending);
        Assert.Equal(2, issue.SubIssueTotal);
        Assert.Equal(1, issue.SubIssueCompleted);

        var pullRequest = Assert.Single(result.Items, item => item.ItemType == PmWorkItemTypeDto.PullRequest);
        Assert.Equal(20, pullRequest.Number);
        Assert.False(pullRequest.IsDraft);
        Assert.True(pullRequest.HasReviewPending);
        Assert.Null(pullRequest.SubIssueTotal);
    }

    [Fact]
    public async Task GetCatalogueAsync_ExcludedRepositoryConfigured_OmitsExcludedRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _pmSettingsService.GetSettingsAsync().Returns(new PmSettingsDto(
            PlanningBoardNodeId: null,
            ExcludedRepositories: ["owner/excluded"],
            Capacity: PmSettingsDefaults.Capacity,
            StallDays: PmSettingsDefaults.StallDays,
            NeglectDays: PmSettingsDefaults.NeglectDays));

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "included", FullName = "owner/included" },
            new Repository { Id = 2, Name = "excluded", FullName = "owner/excluded" },
        ]);

        _gitHubService
            .GetIssuesAsync("owner", "included", OpenItemState, cancellationToken)
            .Returns([CreateIssue(1, "Included issue", ["type/story", "priority/low"])]);
        _gitHubService
            .GetPullRequestsAsync("owner", "included", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "included", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "included", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("owner/included", result.Items[0].RepositoryFullName);
        var summary = Assert.Single(result.RepositorySummaries);
        Assert.Equal("owner/included", summary.FullName);
        Assert.True(summary.IsIncluded);
        await _gitHubService.DidNotReceive().GetIssuesAsync("owner", "excluded", OpenItemState, cancellationToken);
        await _gitHubService.DidNotReceive().GetPullRequestsAsync("owner", "excluded", OpenItemState, cancellationToken);
    }

    [Fact]
    public async Task GetCatalogueAsync_PartialRepositoryFailure_ReturnsFailureAndSuccessfulItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "healthy", FullName = "owner/healthy" },
            new Repository { Id = 2, Name = "broken", FullName = "owner/broken" },
        ]);

        _gitHubService
            .GetIssuesAsync("owner", "healthy", OpenItemState, cancellationToken)
            .Returns([CreateIssue(1, "Healthy issue", ["type/story", "priority/medium"])]);
        _gitHubService
            .GetPullRequestsAsync("owner", "healthy", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "healthy", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "healthy", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        _gitHubService
            .GetIssuesAsync("owner", "broken", OpenItemState, cancellationToken)
            .Throws(new HttpRequestException("Issues unavailable", null, HttpStatusCode.Forbidden));
        _gitHubService
            .GetPullRequestsAsync("owner", "broken", OpenItemState, cancellationToken)
            .Returns([CreatePullRequest(99, "Still loaded", ["type/story", "priority/low"], isDraft: false)]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "broken", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "broken", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, item => item.RepositoryFullName == "owner/healthy");
        Assert.Contains(result.Items, item => item.RepositoryFullName == "owner/broken" && item.Number == 99);

        var failure = Assert.Single(result.Failures);
        Assert.Equal("owner/broken", failure.RepositoryFullName);
        Assert.Equal((int)HttpStatusCode.Forbidden, failure.HttpStatusCode);
        Assert.Contains("Issues:", failure.Message, StringComparison.Ordinal);

        var healthySummary = Assert.Single(result.RepositorySummaries);
        Assert.Equal("owner/healthy", healthySummary.FullName);
        Assert.Equal(1, healthySummary.OpenIssueCount);
        Assert.Equal(0, healthySummary.OpenPullRequestCount);
    }

    [Fact]
    public async Task GetCatalogueAsync_IncludedRepositories_AggregatesIssueAndPullRequestCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repositoryUpdatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var issueUpdatedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        var pullRequestUpdatedAt = DateTimeOffset.Parse("2026-08-18T12:00:00Z");

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "busy", FullName = "owner/busy", UpdatedAt = repositoryUpdatedAt },
            new Repository { Id = 2, Name = "quiet", FullName = "owner/quiet", UpdatedAt = repositoryUpdatedAt },
        ]);

        _gitHubService
            .GetIssuesAsync("owner", "busy", OpenItemState, cancellationToken)
            .Returns(
            [
                CreateIssue(1, "First", ["type/story", "priority/low"], updatedAt: issueUpdatedAt),
                CreateIssue(2, "Second", ["type/bug", "priority/medium"], updatedAt: issueUpdatedAt),
            ]);
        _gitHubService
            .GetPullRequestsAsync("owner", "busy", OpenItemState, cancellationToken)
            .Returns([CreatePullRequest(3, "Open PR", ["type/story", "priority/low"], isDraft: false, updatedAt: pullRequestUpdatedAt)]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "busy", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "busy", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        _gitHubService
            .GetIssuesAsync("owner", "quiet", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetPullRequestsAsync("owner", "quiet", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "quiet", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "quiet", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        Assert.Equal(2, result.RepositorySummaries.Count);

        var busy = Assert.Single(result.RepositorySummaries, summary => summary.FullName == "owner/busy");
        Assert.Equal(2, busy.OpenIssueCount);
        Assert.Equal(1, busy.OpenPullRequestCount);
        Assert.Equal(pullRequestUpdatedAt, busy.LastActivityAt);
        Assert.True(busy.IsIncluded);

        var quiet = Assert.Single(result.RepositorySummaries, summary => summary.FullName == "owner/quiet");
        Assert.Equal(0, quiet.OpenIssueCount);
        Assert.Equal(0, quiet.OpenPullRequestCount);
        Assert.Equal(repositoryUpdatedAt, quiet.LastActivityAt);
        Assert.True(quiet.IsIncluded);
    }

    [Fact]
    public async Task GetCatalogueAsync_GroupingPredicates_AreAvailableOnReturnedLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "repo", FullName = "owner/repo" },
        ]);
        _gitHubService
            .GetIssuesAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns(
            [
                CreateIssue(1, "Urgent ready story", ["type/story", "priority/critical"]),
                CreateIssue(2, "Awaiting triage", ["type/story"]),
                CreateIssue(3, "Blocked", ["type/story", "priority/high", "status/blocked"]),
            ]);
        _gitHubService
            .GetPullRequestsAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "repo", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "repo", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        var urgentReady = result.Items.Single(item => item.Number == 1);
        Assert.True(PmLabelHelpers.IsUrgent(urgentReady.Labels));
        Assert.True(PmLabelHelpers.IsUnblocked(urgentReady.Labels));
        Assert.False(PmLabelHelpers.IsAwaitingTriage(urgentReady.Labels));

        var awaitingTriage = result.Items.Single(item => item.Number == 2);
        Assert.True(PmLabelHelpers.IsAwaitingTriage(awaitingTriage.Labels));

        var blocked = result.Items.Single(item => item.Number == 3);
        Assert.True(PmLabelHelpers.IsBlockedOrDeferred(blocked.Labels));
        Assert.False(PmLabelHelpers.IsUnblocked(blocked.Labels));
    }

    [Fact]
    public async Task GetCatalogueAsync_ReviewMetadataLoadFails_ReturnsItemsWithoutReviewPending()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "repo", FullName = "owner/repo" },
        ]);
        _gitHubService
            .GetIssuesAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetPullRequestsAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns([CreatePullRequest(30, "Needs review", ["type/story", "priority/medium"], isDraft: false)]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "repo", cancellationToken)
            .Throws(new HttpRequestException("Review metadata unavailable"));
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "repo", Arg.Any<IReadOnlyList<int>>(), cancellationToken)
            .Returns([]);

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        var pullRequest = Assert.Single(result.Items);
        Assert.Equal(30, pullRequest.Number);
        Assert.Null(pullRequest.HasReviewPending);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task GetCatalogueAsync_SubIssueSummaryLoadFails_ReturnsItemsWithoutSubIssueCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "repo", FullName = "owner/repo" },
        ]);
        _gitHubService
            .GetIssuesAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns([CreateIssue(10, "Epic parent", ["type/epic", "priority/high"])]);
        _gitHubService
            .GetPullRequestsAsync("owner", "repo", OpenItemState, cancellationToken)
            .Returns([]);
        _gitHubService
            .GetOpenPullRequestReviewMetadataAsync("owner", "repo", cancellationToken)
            .Returns([]);
        _gitHubService
            .GetIssueSubIssueSummariesAsync("owner", "repo", Arg.Is<IReadOnlyList<int>>(numbers => numbers.SequenceEqual(new[] { 10 })), cancellationToken)
            .Throws(new HttpRequestException("Sub-issue summaries unavailable"));

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        var issue = Assert.Single(result.Items);
        Assert.Equal(10, issue.Number);
        Assert.Null(issue.SubIssueTotal);
        Assert.Null(issue.SubIssueCompleted);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task GetCatalogueAsync_BothIssueAndPullRequestLoadFail_ReturnsFailureWithCombinedMessage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(
        [
            new Repository { Id = 1, Name = "broken", FullName = "owner/broken" },
        ]);
        _gitHubService
            .GetIssuesAsync("owner", "broken", OpenItemState, cancellationToken)
            .Throws(new HttpRequestException("Issues unavailable", null, HttpStatusCode.Forbidden));
        _gitHubService
            .GetPullRequestsAsync("owner", "broken", OpenItemState, cancellationToken)
            .Throws(new HttpRequestException("Pull requests unavailable", null, HttpStatusCode.ServiceUnavailable));

        var result = await _sut.GetCatalogueAsync(cancellationToken);

        Assert.Empty(result.Items);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("owner/broken", failure.RepositoryFullName);
        Assert.Contains("Issues:", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Pull requests:", failure.Message, StringComparison.Ordinal);
        Assert.Equal((int)HttpStatusCode.Forbidden, failure.HttpStatusCode);
        Assert.Empty(result.RepositorySummaries);
    }

    private static Issue CreateIssue(
        int number,
        string title,
        IReadOnlyList<string> labelNames,
        string state = "open",
        string? milestoneTitle = null,
        DateTimeOffset? updatedAt = null)
        => new()
        {
            Id = number,
            Number = number,
            Title = title,
            HtmlUrl = $"https://example.test/{number}",
            State = state,
            Labels = labelNames.Select(name => new Label { Name = name }).ToArray(),
            Milestone = milestoneTitle is null
                ? null
                : new Milestone { Number = 4, Title = milestoneTitle },
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow.AddDays(-1),
        };

    private static PullRequest CreatePullRequest(
        int number,
        string title,
        IReadOnlyList<string> labelNames,
        bool isDraft,
        DateTimeOffset? updatedAt = null)
        => new()
        {
            Id = number,
            Number = number,
            Title = title,
            HtmlUrl = $"https://example.test/pr/{number}",
            State = "open",
            IsDraft = isDraft,
            Labels = labelNames.Select(name => new Label { Name = name }).ToArray(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow.AddHours(-2),
        };
}
