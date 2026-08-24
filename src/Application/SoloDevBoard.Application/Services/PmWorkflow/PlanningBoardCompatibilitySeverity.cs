namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Severity for a planning-board compatibility issue.</summary>
public enum PlanningBoardCompatibilitySeverity
{
    /// <summary>Blocks or breaks a core Planning workflow.</summary>
    Error,

    /// <summary>Degrades Planning behaviour but leaves read-only views usable.</summary>
    Warning,
}
