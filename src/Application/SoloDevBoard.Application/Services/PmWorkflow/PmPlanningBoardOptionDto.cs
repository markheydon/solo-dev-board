namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Represents a Projects v2 board that can be selected as the PM planning board.</summary>
/// <param name="Id">The GitHub node identifier for the project board.</param>
/// <param name="Title">The project board title.</param>
/// <param name="OwnerLogin">The login of the project owner.</param>
/// <param name="StatusFieldId">The project status-field node identifier.</param>
public sealed record PmPlanningBoardOptionDto(
    string Id,
    string Title,
    string OwnerLogin,
    string StatusFieldId);
