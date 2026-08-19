using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="DailyFocusBoardStateMapper"/>.</summary>
public sealed class DailyFocusBoardStateMapperTests
{
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

        var result = DailyFocusBoardStateMapper.Map(statusOptions, items, capacity: 8);

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

        var result = DailyFocusBoardStateMapper.Map(statusOptions, items, capacity: 8);

        Assert.Equal(6, result.ActiveLoad);
        Assert.Equal(8, result.Capacity);
    }

    [Fact]
    public void Map_CapacityLessThanOne_UsesDefaultCapacity()
    {
        var result = DailyFocusBoardStateMapper.Map([], [], capacity: 0);

        Assert.Equal(0, result.ActiveLoad);
        Assert.Equal(PmSettingsDefaults.Capacity, result.Capacity);
        Assert.Empty(result.Occupancy);
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

        var result = DailyFocusBoardStateMapper.Map(statusOptions, items, capacity: 8);

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

        var result = DailyFocusBoardStateMapper.Map([], items, capacity: 5);

        Assert.Equal(2, result.Occupancy.Count);
        Assert.Equal("Ready", result.Occupancy[0].StatusName);
        Assert.Equal(2, result.Occupancy[0].Count);
        Assert.Equal("Parked", result.Occupancy[1].StatusName);
        Assert.Equal(1, result.Occupancy[1].Count);
        Assert.Equal(0, result.ActiveLoad);
        Assert.Equal(5, result.Capacity);
    }

    [Fact]
    public void Map_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DailyFocusBoardStateMapper.Map(null!, [], 8));
        Assert.Throws<ArgumentNullException>(() => DailyFocusBoardStateMapper.Map([], null!, 8));
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

    private static ProjectBoardItemDto CreateItem(string? optionId, string? statusName)
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
                "Title",
                "https://github.com/owner/repo/issues/1"),
            DateTimeOffset.UnixEpoch);
    }
}
