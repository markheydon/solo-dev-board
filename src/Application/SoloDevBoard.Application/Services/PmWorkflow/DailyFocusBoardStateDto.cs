namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Read-only Daily Focus board snapshot for occupancy and active load.</summary>
/// <param name="Occupancy">Item counts per discovered Status option, in board-defined order.</param>
/// <param name="ActiveLoad">The number of items whose Status name is <c>Up Next</c> or <c>In Progress</c>.</param>
/// <param name="Capacity">The persisted planning capacity used as the active-load denominator.</param>
/// <param name="ItemCount">The total number of items on the selected board.</param>
public sealed record DailyFocusBoardStateDto(
    IReadOnlyList<DailyFocusOccupancyChipDto> Occupancy,
    int ActiveLoad,
    int Capacity,
    int ItemCount);
