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
    int DuplicateClosuresCount)
{
    /// <summary>Gets grouped detail lines for label actions.</summary>
    public IReadOnlyList<string> LabelActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for milestone assignment actions.</summary>
    public IReadOnlyList<string> MilestoneActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for project-board actions.</summary>
    public IReadOnlyList<string> ProjectActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for duplicate closure actions.</summary>
    public IReadOnlyList<string> DuplicateActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for skip actions, including optional reasons.</summary>
    public IReadOnlyList<string> SkippedActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for items currently skipped for revisit.</summary>
    public IReadOnlyList<string> SkippedItemDetails { get; init; } = [];
}
