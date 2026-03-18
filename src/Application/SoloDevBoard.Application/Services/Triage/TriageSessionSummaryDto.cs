namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents summary data for a triage session.</summary>
/// <param name="TotalItems">The total number of items currently in the active queue.</param>
/// <param name="ProcessedItems">The number of items already processed in the active queue.</param>
/// <param name="RemainingItems">The number of items remaining in the active queue.</param>
/// <param name="SkippedItems">The number of items skipped for later review.</param>
/// <param name="LabelsAppliedCount">The number of label assignment actions recorded.</param>
/// <param name="MilestonesAssignedCount">The number of milestone assignment actions recorded.</param>
/// <param name="ProjectAssignmentsCount">The number of project-board assignment actions recorded.</param>
/// <param name="DuplicateClosuresCount">The number of duplicate closure actions recorded.</param>
public sealed record TriageSessionSummaryDto(
    int TotalItems,
    int ProcessedItems,
    int RemainingItems,
    int SkippedItems,
    int LabelsAppliedCount,
    int MilestonesAssignedCount,
    int ProjectAssignmentsCount,
    int DuplicateClosuresCount);
