namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Planning board options discovered across active repositories.</summary>
/// <param name="Options">Distinct project boards available for selection.</param>
/// <param name="TotalLinkedProjectCount">The total linked project count reported by GitHub across scanned repositories.</param>
/// <param name="InaccessibleLinkedProjectCount">The number of linked boards that could not be read.</param>
public sealed record PlanningProjectBoardDiscoveryDto(
    IReadOnlyList<PlanningBoardOptionDto> Options,
    int TotalLinkedProjectCount,
    int InaccessibleLinkedProjectCount);
