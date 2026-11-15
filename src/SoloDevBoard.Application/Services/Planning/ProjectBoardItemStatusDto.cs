namespace SoloDevBoard.Application.Services.Planning;

/// <summary>DTO for the Status single-select value on a project board item.</summary>
public sealed record ProjectBoardItemStatusDto(
    string OptionId,
    string Name);
