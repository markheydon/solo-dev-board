namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents summary data for a triage session.</summary>
public sealed record TriageSessionSummaryDto(
    int TotalItems,
    int ProcessedItems,
    int RemainingItems,
    int SkippedItems,
    int LabelsAppliedCount,
    int MilestonesAssignedCount,
    int ProjectAssignmentsCount,
    int DuplicateClosuresCount);
