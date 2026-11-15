using NSubstitute;
using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusRecommendationService"/>.</summary>
public sealed class DailyFocusRecommendationServiceTests
{
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService =
        Substitute.For<IPlanningWorkItemCatalogueService>();
    private readonly IProjectItemCatalogueService _projectItemCatalogueService =
        Substitute.For<IProjectItemCatalogueService>();

    [Fact]
    public void Constructor_WorkItemCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusRecommendationService(null!, _projectItemCatalogueService));
    }

    [Fact]
    public void Constructor_ProjectCatalogueIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusRecommendationService(_workItemCatalogueService, null!));
    }

    [Fact]
    public async Task GetRecommendationsAsync_ProjectIdIsBlank_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetRecommendationsAsync(" ", cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task GetRecommendationsAsync_CataloguesReturned_RanksEligibleItems()
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
                ["priority/high"],
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
                "In flight",
                "https://github.com/owner/repo/issues/41",
                "owner/repo",
                ["priority/critical"],
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
                [
                    new ProjectBoardItemDto(
                        "PVTI_41",
                        new ProjectBoardItemStatusDto("opt-in-progress", "In Progress"),
                        null,
                        new ProjectBoardItemContentDto(
                            ProjectBoardItemContentTypeDto.Issue,
                            41,
                            "owner",
                            "repo",
                            "In flight",
                            "https://github.com/owner/repo/issues/41"),
                        DateTimeOffset.UnixEpoch,
                        false),
                ]));

        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        var result = await sut.GetRecommendationsAsync("PVT_board", cancellationToken: cancellationToken);

        var recommended = Assert.Single(result.Recommendations);
        Assert.Equal(40, recommended.Number);
        Assert.Equal("priority/high", recommended.PriorityLabel);
        Assert.Empty(result.Failures);
        await _workItemCatalogueService.Received(1).GetCatalogueAsync(cancellationToken);
        await _projectItemCatalogueService.Received(1).GetCatalogueAsync("PVT_board", cancellationToken);
    }

    [Fact]
    public async Task GetRecommendationsAsync_CatalogueHasFailuresAndNoItems_ThrowsInvalidOperationException()
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

        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetRecommendationsAsync("PVT_board", cancellationToken: cancellationToken));

        Assert.Contains("owner/repo-a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("1 repository failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRecommendationsAsync_CatalogueHasPartialFailures_RanksRemainingItemsAndReturnsFailures()
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
                ["priority/high"],
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
                [
                    new PlanningRepositoryCatalogueFailureDto("owner/repo-b", "Not found", 404),
                    new PlanningRepositoryCatalogueFailureDto("owner/repo-c", "Forbidden", 403),
                ],
                []));
        _projectItemCatalogueService.GetCatalogueAsync("PVT_board", cancellationToken)
            .Returns(new ProjectBoardItemCatalogueDto(
                new ProjectBoardFieldIdsDto("PVTF_status", null),
                [],
                []));

        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        var result = await sut.GetRecommendationsAsync("PVT_board", cancellationToken: cancellationToken);

        var recommended = Assert.Single(result.Recommendations);
        Assert.Equal(40, recommended.Number);
        Assert.Equal(2, result.Failures.Count);
        Assert.Contains(result.Failures, failure => failure.RepositoryFullName == "owner/repo-b");
        Assert.Contains(result.Failures, failure => failure.RepositoryFullName == "owner/repo-c");
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenLimitedToPlanningBoard_RanksOnlyBoardItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            new PlanningWorkItemDto(
                PlanningWorkItemTypeDto.Issue,
                40,
                "Off board",
                "https://github.com/owner/repo/issues/40",
                "owner/repo",
                ["priority/critical"],
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
                "On board",
                "https://github.com/owner/repo/issues/41",
                "owner/repo",
                ["priority/high"],
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
                [
                    new ProjectBoardItemDto(
                        "PVTI_41",
                        new ProjectBoardItemStatusDto("opt-todo", "Todo"),
                        null,
                        new ProjectBoardItemContentDto(
                            ProjectBoardItemContentTypeDto.Issue,
                            41,
                            "owner",
                            "repo",
                            "On board",
                            "https://github.com/owner/repo/issues/41"),
                        DateTimeOffset.UnixEpoch,
                        false),
                ]));

        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        var result = await sut.GetRecommendationsAsync("PVT_board", limitToPlanningBoard: true, cancellationToken);

        var recommended = Assert.Single(result.Recommendations);
        Assert.Equal(41, recommended.Number);
        Assert.Equal("priority/high", recommended.PriorityLabel);
    }
}
