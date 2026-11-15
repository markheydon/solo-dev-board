namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Read-only Daily Focus board snapshot for occupancy, active load, and stalled Up Next items.</summary>
/// <param name="Occupancy">Item counts per discovered Status option, in board-defined order.</param>
/// <param name="ActiveLoad">The number of items whose Status name is <c>Up Next</c> or <c>In Progress</c>.</param>
/// <param name="Capacity">The persisted planning capacity used as the active-load denominator.</param>
/// <param name="ItemCount">The number of mapped Issue and Pull Request items in the catalogue.</param>
/// <param name="StalledUpNextItems">Up Next items at or beyond the stall-day threshold, oldest first.</param>
/// <param name="StallDays">The resolved stall threshold used to build <paramref name="StalledUpNextItems"/>.</param>
public sealed record DailyFocusBoardStateDto(
    IReadOnlyList<DailyFocusOccupancyChipDto> Occupancy,
    int ActiveLoad,
    int Capacity,
    int ItemCount,
    IReadOnlyList<DailyFocusStalledItemDto> StalledUpNextItems,
    int StallDays);
