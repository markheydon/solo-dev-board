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
}
