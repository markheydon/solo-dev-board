using NSubstitute;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusBoardStateService"/>.</summary>
public sealed class DailyFocusBoardStateServiceTests
{
    private readonly IProjectItemCatalogueService _catalogueService = Substitute.For<IProjectItemCatalogueService>();

    [Fact]
    public void Constructor_CatalogueServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new DailyFocusBoardStateService(null!));
    }

    [Fact]
    public async Task GetBoardStateAsync_ProjectIdIsBlank_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new DailyFocusBoardStateService(_catalogueService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetBoardStateAsync(" ", 8, cancellationToken));
    }

    [Fact]
    public async Task GetBoardStateAsync_CatalogueReturned_MapsOccupancyAndActiveLoad()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [
                new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
                new ProjectBoardStatusOptionDto("opt-in-progress", "In Progress"),
            ],
            [
                CreateItem("opt-up-next", "Up Next"),
                CreateItem("opt-in-progress", "In Progress"),
            ]);

        _catalogueService.GetCatalogueAsync("project-id", cancellationToken).Returns(catalogue);

        var sut = new DailyFocusBoardStateService(_catalogueService);

        var result = await sut.GetBoardStateAsync("project-id", 10, cancellationToken);

        Assert.Equal(2, result.ActiveLoad);
        Assert.Equal(10, result.Capacity);
        Assert.Equal(2, result.Occupancy.Count);
        await _catalogueService.Received(1).GetCatalogueAsync("project-id", cancellationToken);
    }

    private static ProjectBoardItemDto CreateItem(string optionId, string statusName) =>
        new(
            "PVTI_item",
            new ProjectBoardItemStatusDto(optionId, statusName),
            FocusOrder: null,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                1,
                "owner",
                "repo",
                "Title",
                "https://github.com/owner/repo/issues/1"),
            DateTimeOffset.UnixEpoch);
}
