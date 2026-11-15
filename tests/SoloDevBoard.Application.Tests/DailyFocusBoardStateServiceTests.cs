using NSubstitute;
using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusBoardStateService"/>.</summary>
public sealed class DailyFocusBoardStateServiceTests
{
    private readonly IProjectItemCatalogueService _catalogueService = Substitute.For<IProjectItemCatalogueService>();
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CatalogueServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusBoardStateService(null!, new FrozenTimeProvider(UtcNow)));
    }

    [Fact]
    public void Constructor_TimeProviderIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _ = new DailyFocusBoardStateService(_catalogueService, null!));
    }

    [Fact]
    public async Task GetBoardStateAsync_ProjectIdIsBlank_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = new DailyFocusBoardStateService(_catalogueService, new FrozenTimeProvider(UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetBoardStateAsync(" ", 8, 3, cancellationToken));
    }

    [Fact]
    public async Task GetBoardStateAsync_CatalogueReturned_MapsOccupancyAndStalledItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto("PVTF_status", null),
            [
                new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
                new ProjectBoardStatusOptionDto("opt-in-progress", "In Progress"),
            ],
            [
                CreateItem("opt-up-next", "Up Next", UtcNow.AddDays(-3), "Stalled story"),
                CreateItem("opt-in-progress", "In Progress", UtcNow.AddDays(-10), "In flight"),
            ]);

        _catalogueService.GetCatalogueAsync("project-id", cancellationToken).Returns(catalogue);

        var sut = new DailyFocusBoardStateService(_catalogueService, new FrozenTimeProvider(UtcNow));

        var result = await sut.GetBoardStateAsync("project-id", 10, 3, cancellationToken);

        Assert.Equal(2, result.ActiveLoad);
        Assert.Equal(10, result.Capacity);
        Assert.Equal(2, result.Occupancy.Count);
        var stalled = Assert.Single(result.StalledUpNextItems);
        Assert.Equal("Stalled story", stalled.Title);
        Assert.Equal(3, stalled.AgeInDays);
        await _catalogueService.Received(1).GetCatalogueAsync("project-id", cancellationToken);
    }

    private static ProjectBoardItemDto CreateItem(
        string optionId,
        string statusName,
        DateTimeOffset activityTimestamp,
        string title) =>
        new(
            "PVTI_item",
            new ProjectBoardItemStatusDto(optionId, statusName),
            FocusOrder: null,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                1,
                "owner",
                "repo",
                title,
                "https://github.com/owner/repo/issues/1"),
            activityTimestamp,
            UsedItemUpdatedAtFallback: false);

    private sealed class FrozenTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FrozenTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
