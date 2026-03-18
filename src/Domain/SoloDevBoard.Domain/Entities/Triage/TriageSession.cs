namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents the state of a single one-at-a-time triage session.</summary>
public sealed record TriageSession : IAggregate
{
    /// <summary>Gets the aggregate identifier for the session.</summary>
    public int Id { get; init; }

    /// <summary>Gets the session identifier.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Gets the owner login for the session repository.</summary>
    public string OwnerLogin { get; init; } = string.Empty;

    /// <summary>Gets the repository name for this session.</summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether pull requests are included in the session queue.</summary>
    public bool IncludePullRequests { get; init; }

    /// <summary>Gets the queue of triage items for this session.</summary>
    public IReadOnlyList<TriageItem> Queue { get; init; } = [];

    /// <summary>Gets the zero-based index of the active item in the queue.</summary>
    public int CurrentIndex { get; init; }

    /// <summary>Gets the items that were skipped and marked for revisit.</summary>
    public IReadOnlyList<TriageItem> SkippedItems { get; init; } = [];

    /// <summary>Gets the action history for this session.</summary>
    public IReadOnlyList<TriageAction> ActionHistory { get; init; } = [];

    /// <summary>Gets computed progress information for the current state.</summary>
    public TriageSessionProgress Progress { get; init; } = new();

    /// <summary>Gets summary information for the current state.</summary>
    public TriageSessionSummary Summary { get; init; } = new();

    /// <summary>Gets the timestamp when the session started.</summary>
    public DateTimeOffset StartedAt { get; init; }
}
