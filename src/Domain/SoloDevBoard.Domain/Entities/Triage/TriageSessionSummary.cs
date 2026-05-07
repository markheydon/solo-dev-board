namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents summary data for a completed or in-flight triage session.</summary>
public sealed record TriageSessionSummary : IAggregate
{
    /// <summary>Gets the aggregate identifier for this summary snapshot.</summary>
    public int Id { get; init; }

    /// <summary>Gets the total number of items in the session queue.</summary>
    public int TotalItems { get; init; }

    /// <summary>Gets the number of processed items.</summary>
    public int ProcessedItems { get; init; }

    /// <summary>Gets the number of remaining items.</summary>
    public int RemainingItems { get; init; }

    /// <summary>Gets the number of items that were skipped.</summary>
    public int SkippedItems { get; init; }

    /// <summary>Gets the number of label actions recorded.</summary>
    public int LabelsAppliedCount { get; init; }

    /// <summary>Gets the number of milestone assignment actions recorded.</summary>
    public int MilestonesAssignedCount { get; init; }

    /// <summary>Gets the number of project-board assignment actions recorded.</summary>
    public int ProjectAssignmentsCount { get; init; }

    /// <summary>Gets the number of duplicate closure actions recorded.</summary>
    public int DuplicateClosuresCount { get; init; }

    /// <summary>Gets grouped detail lines for label actions.</summary>
    public IReadOnlyList<string> LabelActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for milestone assignment actions.</summary>
    public IReadOnlyList<string> MilestoneActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for project-board actions.</summary>
    public IReadOnlyList<string> ProjectActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for duplicate closure actions.</summary>
    public IReadOnlyList<string> DuplicateActionDetails { get; init; } = [];

    /// <summary>Gets grouped detail lines for items currently skipped for revisit.</summary>
    public IReadOnlyList<string> SkippedItemDetails { get; init; } = [];
}
