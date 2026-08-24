namespace SoloDevBoard.Application.Services.Planning;

/// <summary>DTO for discovered GitHub Project v2 field identifiers.</summary>
public sealed record ProjectBoardFieldIdsDto(
    string StatusFieldId,
    string? FocusOrderFieldId);
