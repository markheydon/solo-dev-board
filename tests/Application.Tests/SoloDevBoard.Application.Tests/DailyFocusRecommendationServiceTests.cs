using NSubstitute;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusRecommendationService"/>.</summary>
public sealed class DailyFocusRecommendationServiceTests
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService =
        Substitute.For<IPmWorkItemCatalogueService>();
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
            sut.GetRecommendationsAsync(" ", cancellationToken));
    }

    [Fact]
    public async Task GetRecommendationsAsync_CataloguesReturned_RanksEligibleItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var updated = DateTimeOffset.Parse("2026-08-18T00:00:00Z");
        var workItems = new[]
        {
            new PmWorkItemDto(
                PmWorkItemTypeDto.Issue,
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
            new PmWorkItemDto(
                PmWorkItemTypeDto.Issue,
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
            .Returns(new PmWorkItemCatalogueResultDto(workItems, []));
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
                        DateTimeOffset.UnixEpoch),
                ]));

        var sut = new DailyFocusRecommendationService(_workItemCatalogueService, _projectItemCatalogueService);

        var result = await sut.GetRecommendationsAsync("PVT_board", cancellationToken);

        var recommended = Assert.Single(result);
        Assert.Equal(40, recommended.Number);
        Assert.Equal("priority/high", recommended.PriorityLabel);
        await _workItemCatalogueService.Received(1).GetCatalogueAsync(cancellationToken);
        await _projectItemCatalogueService.Received(1).GetCatalogueAsync("PVT_board", cancellationToken);
    }
}
