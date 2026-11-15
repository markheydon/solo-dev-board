namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Read-only occupancy chip for a discovered Status option.</summary>
/// <param name="StatusName">The Status option display name.</param>
/// <param name="Count">The number of board items currently in this Status.</param>
public sealed record DailyFocusOccupancyChipDto(string StatusName, int Count);
