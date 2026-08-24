using NSubstitute;
using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BacklogReviewService"/>.</summary>
public sealed class BacklogReviewServiceTests
{
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService =
        Substitute.For<IPlanningWorkItemCatalogueService>();
    private readonly IProjectItemCatalogueService _projectItemCatalogueService =
        Substitute.For<IProjectItemCatalogueService>();
    private readonly IPlanningSettingsService _pmSettingsService = Substitute.For<IPlanningSettingsService>();

    public BacklogReviewServiceTests()
    {
        _pmSettingsService.GetSettingsAsync().Returns(PlanningSettingsDefaults.Create());
    }

    [Fact]
    public void Constructor_WorkItemCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new BacklogReviewService(null!, _projectItemCatalogueService, _pmSettingsService));
    }

    [Fact]
    public void Constructor_ProjectCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new BacklogReviewService(_workItemCatalogueService, null!, _pmSettingsService));
    }

    [Fact]
    public void Constructor_SettingsServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new BacklogReviewService(_workItemCatalogueService, _projectItemCatalogueService, null!));
    }

    [Fact]
    public async Task GetBacklogAsync_ProjectIdIsBlank_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new BacklogReviewService(_workItemCatalogueService, _projectItemCatalogueService, _pmSettingsService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetBacklogAsync(" ", cancellationToken));
    }

    [Fact]
    public async Task GetBacklogAsync_CataloguesReturned_GroupsItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            new PlanningWorkItemDto(
                PlanningWorkItemTypeDto.Issue,
                40,
                "Do this",
                "https://github.com/owner/repo/issues/40",
                "owner/repo",
                ["priority/high", "type/story"],
                null,
                null,
                updated,
                updated,
                null,
                null,
                null,
                null),
            new PlanningWorkItemDto(
                PlanningWorkItemTypeDto.Issue,
                41,
                "Parked",
                "https://github.com/owner/repo/issues/41",
                "owner/repo",
                ["priority/medium", "status/blocked"],
                null,
                null,
                updated,
                updated,
                null,
                null,
                null,
                null),
        };

        _workItemCatalogueService.GetCatalogueAsync(cancellationToken)
            .Returns(new PlanningWorkItemCatalogueResultDto(workItems, [], []));
        _projectItemCatalogueService.GetCatalogueAsync("PVT_board", cancellationToken)
            .Returns(new ProjectBoardItemCatalogueDto(
                new ProjectBoardFieldIdsDto("PVTF_status", null),
                [],
                []));

        var sut = new BacklogReviewService(_workItemCatalogueService, _projectItemCatalogueService, _pmSettingsService);

        var result = await sut.GetBacklogAsync("PVT_board", cancellationToken);

        Assert.Equal(40, Assert.Single(result.Urgent).Number);
        Assert.Empty(result.ReadyToStart);
        Assert.Equal(41, Assert.Single(result.BlockedOrDeferred).Number);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task GetBacklogAsync_CatalogueHasFailuresAndNoItems_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _workItemCatalogueService.GetCatalogueAsync(cancellationToken)
            .Returns(new PlanningWorkItemCatalogueResultDto(
                [],
                [new PlanningRepositoryCatalogueFailureDto("owner/repo-a", "GitHub unavailable", 500)],
                []));
        _projectItemCatalogueService.GetCatalogueAsync("PVT_board", cancellationToken)
            .Returns(new ProjectBoardItemCatalogueDto(
                new ProjectBoardFieldIdsDto("PVTF_status", null),
                [],
                []));

        var sut = new BacklogReviewService(_workItemCatalogueService, _projectItemCatalogueService, _pmSettingsService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetBacklogAsync("PVT_board", cancellationToken));

        Assert.Contains("owner/repo-a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("1 repository failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetBacklogAsync_CatalogueHasPartialFailures_GroupsRemainingItemsAndReturnsFailures()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            new PlanningWorkItemDto(
                PlanningWorkItemTypeDto.Issue,
                40,
                "Do this",
                "https://github.com/owner/repo/issues/40",
                "owner/repo",
                ["priority/medium", "type/story"],
                null,
                null,
                updated,
                updated,
                null,
                null,
                null,
                null),
        };

        _workItemCatalogueService.GetCatalogueAsync(cancellationToken)
            .Returns(new PlanningWorkItemCatalogueResultDto(
                workItems,
                [new PlanningRepositoryCatalogueFailureDto("owner/repo-b", "Not found", 404)],
                []));
        _projectItemCatalogueService.GetCatalogueAsync("PVT_board", cancellationToken)
            .Returns(new ProjectBoardItemCatalogueDto(
                new ProjectBoardFieldIdsDto("PVTF_status", null),
                [],
                []));

        var sut = new BacklogReviewService(_workItemCatalogueService, _projectItemCatalogueService, _pmSettingsService);

        var result = await sut.GetBacklogAsync("PVT_board", cancellationToken);

        Assert.Equal(40, Assert.Single(result.ReadyToStart).Number);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("owner/repo-b", failure.RepositoryFullName);
    }
}
