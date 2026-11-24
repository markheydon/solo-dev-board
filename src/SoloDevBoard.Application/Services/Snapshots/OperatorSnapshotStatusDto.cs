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

    /// <summary>What started the current or last run, if known.</summary>
    public OperatorSnapshotCollectionTrigger? Trigger { get; init; }

    /// <summary>Whether scheduled collection is modelled.</summary>
    public OperatorSnapshotSchedulePosture SchedulePosture { get; init; }

    /// <summary>UTC date when waiting to start today's daily occasion, if any.</summary>
    public string? QueuedForOccasion { get; init; }

    /// <summary>Next 05:00 UTC that would queue them, if any.</summary>
    public DateTimeOffset? NextOccasionAtUtc { get; init; }

    /// <summary>When <see cref="NextOccasionAtUtc"/> falls on the UTC calendar day after evaluation.</summary>
    public bool NextOccasionOnFollowingUtcDay { get; init; }

    /// <summary>Why no next occasion is scheduled, when applicable.</summary>
    public OperatorSnapshotNoScheduleReason NoScheduleReason { get; init; }
}
