namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a project-board option available for triage placement.</summary>
/// <param name="Id">The GitHub node identifier for the project board.</param>
/// <param name="Title">The project board title.</param>
/// <param name="OwnerLogin">The login of the project owner.</param>
/// <param name="StatusFieldId">The project status-field node identifier.</param>
/// <param name="StatusOptions">The selectable project status options.</param>
public sealed record TriageProjectBoardOptionDto(
    string Id,
    string Title,
    string OwnerLogin,
    string StatusFieldId,
    IReadOnlyList<TriageProjectBoardStatusOptionDto> StatusOptions);
