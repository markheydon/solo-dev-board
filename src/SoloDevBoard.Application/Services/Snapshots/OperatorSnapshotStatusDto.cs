namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Status returned to the Repositories page (no repository list).</summary>
public sealed class OperatorSnapshotStatusDto
{
    /// <summary>Current operator-visible state.</summary>
    public OperatorSnapshotStatusKind Kind { get; init; }

    /// <summary>When the current run started, if known.</summary>
    public DateTimeOffset? RunStartedAt { get; init; }

    /// <summary>When the current run finished, if known.</summary>
    public DateTimeOffset? RunFinishedAt { get; init; }

    /// <summary>Repositories recorded during the current run.</summary>
    public int RepositoriesRecordedThisRun { get; init; }

    /// <summary>Finish time of the kept successful snapshot, if any.</summary>
    public DateTimeOffset? LastSuccessfulCollectionAt { get; init; }

    /// <summary>Repository entries in the kept successful snapshot.</summary>
    public int SnapshotRepositoryCount { get; init; }

    /// <summary>Short reason for failure or interruption.</summary>
    public OperatorSnapshotRunReason? Reason { get; init; }

    /// <summary>One-line detail for unexpected failures.</summary>
    public string? Detail { get; init; }

    /// <summary>Where the snapshot is kept for operator copy.</summary>
    public OperatorSnapshotDurabilityKind DurabilityKind { get; init; }
}
