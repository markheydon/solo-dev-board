namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a supported GitHub Project v2 board available for column migration.</summary>
/// <param name="Id">The GitHub node identifier for the project board.</param>
/// <param name="Title">The project board title.</param>
public sealed record MigrationProjectBoardOptionDto(
    string Id,
    string Title);
