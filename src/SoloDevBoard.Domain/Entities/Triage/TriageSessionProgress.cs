namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents computed session progress data for triage flow.</summary>
public sealed record TriageSessionProgress : IAggregate
{
    /// <summary>Gets the aggregate identifier for this progress snapshot.</summary>
    public int Id { get; init; }

    /// <summary>Gets the total number of items in the session queue.</summary>
    public int TotalItems { get; init; }

    /// <summary>Gets the number of items already traversed in the queue.</summary>
    public int ProcessedItems { get; init; }

    /// <summary>Gets the number of items remaining in the queue.</summary>
    public int RemainingItems { get; init; }

    /// <summary>Gets the number of items currently marked as skipped.</summary>
    public int SkippedItems { get; init; }
}
