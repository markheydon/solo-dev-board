using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusBoardStateMapper"/>.</summary>
public sealed class DailyFocusBoardStateMapperTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Map_DiscoveredStatusOptions_IncludesEmptyColumnsInBoardOrder()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
            new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
            new ProjectBoardStatusOptionDto("opt-in-progress", "In Progress"),
            new ProjectBoardStatusOptionDto("opt-blocked", "Blocked"),
            new ProjectBoardStatusOptionDto("opt-ice-box", "Ice Box"),
            new ProjectBoardStatusOptionDto("opt-done", "Done"),
            new ProjectBoardStatusOptionDto("opt-waiting", "Waiting on review"),
        };

        var items = new[]
        {
            CreateItem("opt-todo", "Todo"),
            CreateItem("opt-todo", "Todo"),
            CreateItem("opt-up-next", "Up Next"),
            CreateItem("opt-in-progress", "In Progress"),
            CreateItem("opt-in-progress", "In Progress"),
            CreateItem("opt-waiting", "Waiting on review"),
        };

        var result = Map(statusOptions, items, capacity: 8);

        Assert.Equal(7, result.Occupancy.Count);
        Assert.Equal("Todo", result.Occupancy[0].StatusName);
        Assert.Equal(2, result.Occupancy[0].Count);
        Assert.Equal("Up Next", result.Occupancy[1].StatusName);
        Assert.Equal(1, result.Occupancy[1].Count);
        Assert.Equal("In Progress", result.Occupancy[2].StatusName);
        Assert.Equal(2, result.Occupancy[2].Count);
        Assert.Equal("Blocked", result.Occupancy[3].StatusName);
        Assert.Equal(0, result.Occupancy[3].Count);
        Assert.Equal("Ice Box", result.Occupancy[4].StatusName);
        Assert.Equal(0, result.Occupancy[4].Count);
        Assert.Equal("Done", result.Occupancy[5].StatusName);
        Assert.Equal(0, result.Occupancy[5].Count);
        Assert.Equal("Waiting on review", result.Occupancy[6].StatusName);
        Assert.Equal(1, result.Occupancy[6].Count);
        Assert.Equal(6, result.ItemCount);
        Assert.Empty(result.StalledUpNextItems);
    }

    [Fact]
    public void Map_UpNextAndInProgressItems_ActiveLoadEqualsSum()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
            new ProjectBoardStatusOptionDto("opt-in-progress", "In Progress"),
            new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
        };

        var items = new[]
        {
            CreateItem("opt-up-next", "Up Next"),
            CreateItem("opt-up-next", "Up Next"),
            CreateItem("opt-up-next", "Up Next"),
            CreateItem("opt-up-next", "Up Next"),
            CreateItem("opt-in-progress", "In Progress"),
            CreateItem("opt-in-progress", "In Progress"),
            CreateItem("opt-todo", "Todo"),
        };

        var result = Map(statusOptions, items, capacity: 8);

        Assert.Equal(6, result.ActiveLoad);
        Assert.Equal(8, result.Capacity);
    }

    [Fact]
    public void Map_CapacityLessThanOne_UsesDefaultCapacity()
    {
        var result = Map([], [], capacity: 0);

        Assert.Equal(0, result.ActiveLoad);
        Assert.Equal(PlanningSettingsDefaults.Capacity, result.Capacity);
        Assert.Empty(result.Occupancy);
        Assert.Equal(PlanningSettingsDefaults.StallDays, result.StallDays);
    }

    [Fact]
    public void Map_ItemsWithoutStatus_AddsNoStatusChip()
    {
        var statusOptions = new[]
        {
            new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
        };

        var items = new[]
        {
            CreateItem("opt-todo", "Todo"),
            CreateItem(null, null),
        };

        var result = Map(statusOptions, items, capacity: 8);

        Assert.Equal(2, result.Occupancy.Count);
        Assert.Equal("Todo", result.Occupancy[0].StatusName);
        Assert.Equal(1, result.Occupancy[0].Count);
        Assert.Equal(DailyFocusBoardStateMapper.NoStatusChipName, result.Occupancy[1].StatusName);
        Assert.Equal(1, result.Occupancy[1].Count);
        Assert.Equal(0, result.ActiveLoad);
    }

    [Fact]
    public void Map_NoDiscoveredOptions_DerivesChipsFromItemStatusNames()
    {
        var items = new[]
        {
            CreateItem("opt-ready", "Ready"),
            CreateItem("opt-ready", "Ready"),
            CreateItem("opt-parked", "Parked"),
        };

        var result = Map([], items, capacity: 5);

        Assert.Equal(2, result.Occupancy.Count);
        Assert.Equal("Ready", result.Occupancy[0].StatusName);
        Assert.Equal(2, result.Occupancy[0].Count);
        Assert.Equal("Parked", result.Occupancy[1].StatusName);
        Assert.Equal(1, result.Occupancy[1].Count);
        Assert.Equal(0, result.ActiveLoad);
        Assert.Equal(5, result.Capacity);
    }

    [Fact]
    public void Map_UpNextItemAgedTwoDays_IsNotStalled()
    {
        var items = new[]
        {
            CreateItem(
                "opt-up-next",
                "Up Next",
                activityTimestamp: UtcNow.AddDays(-2),
                title: "Day two item"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8);

        Assert.Empty(result.StalledUpNextItems);
    }

    [Fact]
    public void Map_UpNextItemAgedExactlyThreeDays_IsStalled()
    {
        var items = new[]
        {
            CreateItem(
                "opt-up-next",
                "Up Next",
                activityTimestamp: UtcNow.AddDays(-3),
                title: "Day three item"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8);

        var stalled = Assert.Single(result.StalledUpNextItems);
        Assert.Equal("Day three item", stalled.Title);
        Assert.Equal(3, stalled.AgeInDays);
        Assert.False(stalled.UsedUpdatedAtFallback);
        Assert.Equal(PlanningSettingsDefaults.StallDays, result.StallDays);
    }

    [Fact]
    public void Map_UpNextItemJustUnderThreeDays_IsNotStalled()
    {
        var items = new[]
        {
            CreateItem(
                "opt-up-next",
                "Up Next",
                activityTimestamp: UtcNow.AddDays(-3).AddHours(1),
                title: "Just under three days"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8);

        Assert.Empty(result.StalledUpNextItems);
    }

    [Fact]
    public void Map_ConfigurableStallDays_UsesInclusiveThreshold()
    {
        var items = new[]
        {
            CreateItem("opt-up-next", "Up Next", UtcNow.AddDays(-4), title: "Four days"),
            CreateItem("opt-up-next", "Up Next", UtcNow.AddDays(-5), title: "Five days"),
            CreateItem("opt-todo", "Todo", UtcNow.AddDays(-10), title: "Todo aged"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8, stallDays: 5);

        var stalled = Assert.Single(result.StalledUpNextItems);
        Assert.Equal("Five days", stalled.Title);
        Assert.Equal(5, stalled.AgeInDays);
        Assert.Equal(5, result.StallDays);
    }

    [Fact]
    public void Map_UpNextItemWithUnixEpochActivity_IsNotStalled()
    {
        var items = new[]
        {
            CreateItem(
                "opt-up-next",
                "Up Next",
                DateTimeOffset.UnixEpoch,
                usedFallback: true,
                title: "Unknown stall clock"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8);

        Assert.Empty(result.StalledUpNextItems);
    }

    [Fact]
    public void Map_StalledItemWithUpdatedAtFallback_SetsFallbackFlag()
    {
        var items = new[]
        {
            CreateItem(
                "opt-up-next",
                "Up Next",
                UtcNow.AddDays(-4),
                usedFallback: true,
                title: "Fallback item",
                url: "https://github.com/owner/repo/issues/42"),
        };

        var result = Map(UpNextOptions(), items, capacity: 8);

        var stalled = Assert.Single(result.StalledUpNextItems);
        Assert.True(stalled.UsedUpdatedAtFallback);
        Assert.Equal("https://github.com/owner/repo/issues/42", stalled.Url);
    }

    [Fact]
    public void Map_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Map(null!, [], capacity: 8));
        Assert.Throws<ArgumentNullException>(() => Map([], null!, capacity: 8));
    }

    [Theory]
    [InlineData("Up Next", true)]
    [InlineData("up next", true)]
    [InlineData("In Progress", true)]
    [InlineData("Todo", false)]
    [InlineData(null, false)]
    public void IsActiveLoadStatus_StatusName_ExpectedOutcome(string? statusName, bool expected)
    {
        Assert.Equal(expected, DailyFocusBoardStateMapper.IsActiveLoadStatus(statusName));
    }

    [Theory]
    [InlineData(-2, 2)]
    [InlineData(-3, 3)]
    [InlineData(1, 0)]
    public void GetAgeInDays_Elapsed_ExpectedWholeDays(int daysFromNow, int expectedAge)
    {
        Assert.Equal(expectedAge, DailyFocusBoardStateMapper.GetAgeInDays(UtcNow.AddDays(daysFromNow), UtcNow));
    }

    [Fact]
    public void GetAgeInDays_UnixEpoch_ReturnsZero()
    {
        Assert.Equal(0, DailyFocusBoardStateMapper.GetAgeInDays(DateTimeOffset.UnixEpoch, UtcNow));
        Assert.False(DailyFocusBoardStateMapper.HasStallClock(DateTimeOffset.UnixEpoch));
    }

    private static DailyFocusBoardStateDto Map(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions,
        IReadOnlyList<ProjectBoardItemDto> items,
        int capacity,
        int stallDays = PlanningSettingsDefaults.StallDays) =>
        DailyFocusBoardStateMapper.Map(statusOptions, items, capacity, stallDays, UtcNow);

    private static IReadOnlyList<ProjectBoardStatusOptionDto> UpNextOptions() =>
    [
        new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"),
        new ProjectBoardStatusOptionDto("opt-todo", "Todo"),
    ];

    private static ProjectBoardItemDto CreateItem(
        string? optionId,
        string? statusName,
        DateTimeOffset? activityTimestamp = null,
        bool usedFallback = false,
        string title = "Title",
        string url = "https://github.com/owner/repo/issues/1")
    {
        var status = optionId is null || statusName is null
            ? null
            : new ProjectBoardItemStatusDto(optionId, statusName);

        return new ProjectBoardItemDto(
            "PVTI_item",
            status,
            FocusOrder: null,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                1,
                "owner",
                "repo",
                title,
                url),
            activityTimestamp ?? UtcNow,
            usedFallback);
    }
}
