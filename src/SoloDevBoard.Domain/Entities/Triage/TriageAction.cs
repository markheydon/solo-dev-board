namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a single action taken during a triage session.</summary>
public sealed record TriageAction : IAggregate
{
    /// <summary>Gets the aggregate identifier for this triage action.</summary>
    public int Id { get; init; }

    /// <summary>Gets the action type.</summary>
    public TriageActionType ActionType { get; init; }

    /// <summary>Gets the type of item the action was applied to.</summary>
    public TriageItemType ItemType { get; init; }

    /// <summary>Gets the repository-scoped item number targeted by the action.</summary>
    public int ItemNumber { get; init; }

    /// <summary>Gets the repository full name in owner/repository format.</summary>
    public string RepositoryFullName { get; init; } = string.Empty;

    /// <summary>Gets optional action detail text used by the UI.</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Gets the timestamp when the action was recorded.</summary>
    public DateTimeOffset OccurredAt { get; init; }
}
