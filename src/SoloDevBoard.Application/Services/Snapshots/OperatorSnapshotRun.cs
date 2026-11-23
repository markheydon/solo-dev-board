namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Run section of an operator snapshot document.</summary>
public sealed class OperatorSnapshotRun
{
    /// <summary>Run status.</summary>
    public OperatorSnapshotRunStatus Status { get; set; }

    /// <summary>When this run started (UTC).</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>When this run finished, or null while in progress.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Repositories recorded during this run.</summary>
    public int RepositoriesRecorded { get; set; }

    /// <summary>Short failure or interruption reason, if any.</summary>
    public OperatorSnapshotRunReason? Reason { get; set; }

    /// <summary>One-line diagnostic detail for unexpected failures only.</summary>
    public string? Detail { get; set; }

    /// <summary>Who started this run, or null when absent on older documents.</summary>
    public OperatorSnapshotRunStartedBy? StartedBy { get; set; }
}
