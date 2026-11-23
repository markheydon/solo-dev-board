namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Persisted operator snapshot document (JSON shape).</summary>
public sealed class OperatorSnapshotDocument
{
    /// <summary>Document schema version.</summary>
    public int SchemaVersion { get; set; } = OperatorSnapshotDocumentConstants.CurrentSchemaVersion;

    /// <summary>Sanitised GitHub login for the operator.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary>Monotonic revision used for conditional writes.</summary>
    public long Revision { get; set; }

    /// <summary>UTC time of the last successful collection, or null if none.</summary>
    public DateTimeOffset? LastSuccessfulCollectionAt { get; set; }

    /// <summary>Current run state only (no history).</summary>
    public OperatorSnapshotRun Run { get; set; } = new();

    /// <summary>Entries from the latest successful walk.</summary>
    public List<OperatorSnapshotRepositoryEntry> Repositories { get; set; } = [];

    /// <summary>Reserved object for later feature facts.</summary>
    public Dictionary<string, object?> LaterFacts { get; set; } = [];

    /// <summary>What started the current run, or absent for legacy documents.</summary>
    public OperatorSnapshotCollectionTrigger? Trigger { get; set; }

    /// <summary>UTC date (<c>yyyy-MM-dd</c>) when queued for a daily occasion, or null.</summary>
    public string? QueuedOccasion { get; set; }

    /// <summary>True only while a daily pass is executing inside the worker.</summary>
    public bool PassExecuting { get; set; }

    /// <summary>Renewed while <see cref="PassExecuting"/> is true; liveness of the pass.</summary>
    public DateTimeOffset? PassLeaseUntil { get; set; }

    /// <summary>Daily attempt count, up to four (first pass plus three further tries).</summary>
    public int? AttemptsUsed { get; set; }
}
