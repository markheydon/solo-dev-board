namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>A single planning-board setup issue discovered for the selected project board.</summary>
/// <param name="Code">Stable identifier for the issue.</param>
/// <param name="Severity">Whether the issue blocks core workflows or only degrades them.</param>
/// <param name="Title">Short summary shown in chrome and detail views.</param>
/// <param name="Detail">Explanation of what Planning expects and which workflows are affected.</param>
public sealed record PlanningBoardCompatibilityIssueDto(
    string Code,
    PlanningBoardCompatibilitySeverity Severity,
    string Title,
    string Detail);
