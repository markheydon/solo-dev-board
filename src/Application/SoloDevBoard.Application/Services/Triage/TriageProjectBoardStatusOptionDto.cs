namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a project-board status option at the Application→App boundary.</summary>
/// <param name="Id">The GitHub node identifier for the status option.</param>
/// <param name="Name">The display name for the status option.</param>
public sealed record TriageProjectBoardStatusOptionDto(string Id, string Name);
