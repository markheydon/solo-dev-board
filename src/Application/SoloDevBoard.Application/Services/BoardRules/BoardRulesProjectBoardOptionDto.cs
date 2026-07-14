namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents a supported GitHub Project v2 board available for visualisation.</summary>
/// <param name="Id">The GitHub node identifier for the project board.</param>
/// <param name="Title">The project board title.</param>
/// <param name="OwnerLogin">The login of the project owner.</param>
public sealed record BoardRulesProjectBoardOptionDto(
    string Id,
    string Title,
    string OwnerLogin);
