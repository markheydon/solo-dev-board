using NSubstitute;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusStalledReviewService"/>.</summary>
public sealed class DailyFocusStalledReviewServiceTests
{
    private readonly IProjectItemCatalogueService _projectCatalogue = Substitute.For<IProjectItemCatalogueService>();
    private readonly IPmWorkItemCatalogueService _workCatalogue = Substitute.For<IPmWorkItemCatalogueService>();
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;

    [Fact]
    public void Constructor_ProjectCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusStalledReviewService(null!, _workCatalogue));
    }

    [Fact]
    public void Constructor_WorkCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusStalledReviewService(_projectCatalogue, null!));
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_ProjectIdIsBlank_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetStalledReviewPullRequestsAsync(" ", 3, [], cancellationToken));
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_ExcludedRepositoriesIsNull_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.GetStalledReviewPullRequestsAsync("project-id", 3, null!, cancellationToken));
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_BoardHasInReviewColumn_UsesColumnTimeAndSkipsWorkCatalogue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [new ProjectBoardStatusOptionDto("opt-review", "In Review")],
            [
                new ProjectBoardItemDto(
                    "PVTI_item",
                    new ProjectBoardItemStatusDto("opt-review", "In Review"),
                    FocusOrder: null,
                    new ProjectBoardItemContentDto(
                        ProjectBoardItemContentTypeDto.PullRequest,
                        12,
                        "owner",
                        "repo",
                        "Stalled PR",
                        "https://github.com/owner/repo/pull/12"),
                    UtcNow.AddDays(-5),
                    false),
            ]);
        _projectCatalogue.GetCatalogueAsync("project-id", cancellationToken).Returns(catalogue);

        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        var result = await sut.GetStalledReviewPullRequestsAsync("project-id", 3, [], cancellationToken);

        Assert.True(result.UsedInReviewColumn);
        var stalled = Assert.Single(result.Items);
        Assert.Equal(12, stalled.Number);
        Assert.Equal("owner/repo", stalled.RepositoryFullName);
        await _workCatalogue.DidNotReceive().GetCatalogueAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_NoInReviewColumn_UsesPendingReviewCatalogue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var boardCatalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [new ProjectBoardStatusOptionDto("opt-todo", "Todo")],
            []);
        _projectCatalogue.GetCatalogueAsync("project-id", cancellationToken).Returns(boardCatalogue);

        var workItem = new PmWorkItemDto(
            PmWorkItemTypeDto.PullRequest,
            21,
            "Pending review",
            "https://github.com/owner/repo/pull/21",
            "owner/repo",
            [],
            null,
            null,
            UtcNow.AddDays(-4),
            UtcNow,
            IsDraft: false,
            HasReviewPending: true,
            SubIssueTotal: null,
            SubIssueCompleted: null);
        _workCatalogue.GetCatalogueAsync(cancellationToken)
            .Returns(new PmWorkItemCatalogueResultDto([workItem], [], []));

        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        var result = await sut.GetStalledReviewPullRequestsAsync("project-id", 3, [], cancellationToken);

        Assert.False(result.UsedInReviewColumn);
        var stalled = Assert.Single(result.Items);
        Assert.Equal(21, stalled.Number);
        await _workCatalogue.Received(1).GetCatalogueAsync(cancellationToken);
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_NoInReviewColumnAndExcludedRepository_OmitsExcludedPullRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var boardCatalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [new ProjectBoardStatusOptionDto("opt-todo", "Todo")],
            []);
        _projectCatalogue.GetCatalogueAsync("project-id", cancellationToken).Returns(boardCatalogue);

        var excludedItem = new PmWorkItemDto(
            PmWorkItemTypeDto.PullRequest,
            21,
            "Pending review",
            "https://github.com/owner/skipped/pull/21",
            "owner/skipped",
            [],
            null,
            null,
            UtcNow.AddDays(-4),
            UtcNow,
            IsDraft: false,
            HasReviewPending: true,
            SubIssueTotal: null,
            SubIssueCompleted: null);
        _workCatalogue.GetCatalogueAsync(cancellationToken)
            .Returns(new PmWorkItemCatalogueResultDto([excludedItem], [], []));

        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        var result = await sut.GetStalledReviewPullRequestsAsync(
            "project-id",
            3,
            ["owner/skipped"],
            cancellationToken);

        Assert.False(result.UsedInReviewColumn);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetStalledReviewPullRequestsAsync_WorkCatalogueHasFailures_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var boardCatalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [new ProjectBoardStatusOptionDto("opt-todo", "Todo")],
            []);
        _projectCatalogue.GetCatalogueAsync("project-id", cancellationToken).Returns(boardCatalogue);
        _workCatalogue.GetCatalogueAsync(cancellationToken)
            .Returns(new PmWorkItemCatalogueResultDto(
                [],
                [new PmRepositoryCatalogueFailureDto("owner/repo", "GitHub unavailable", 502)],
                []));

        var sut = new DailyFocusStalledReviewService(_projectCatalogue, _workCatalogue);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetStalledReviewPullRequestsAsync("project-id", 3, [], cancellationToken));

        Assert.Contains("failed to load", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
