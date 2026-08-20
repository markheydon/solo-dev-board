namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Supported project board options and visibility metadata for column migration.</summary>
/// <param name="Options">Supported GitHub Project v2 boards that expose a Status field.</param>
/// <param name="TotalLinkedProjectCount">The number of project boards GitHub reports as linked to the repository.</param>
/// <param name="InaccessibleLinkedProjectCount">Linked project boards that could not be read with the current credentials.</param>
public sealed record MigrationProjectBoardDiscoveryDto(
    IReadOnlyList<MigrationProjectBoardOptionDto> Options,
    int TotalLinkedProjectCount,
    int InaccessibleLinkedProjectCount);
