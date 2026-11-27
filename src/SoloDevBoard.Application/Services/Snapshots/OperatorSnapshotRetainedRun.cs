namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>
/// Terminal run and trigger preserved while a daily occasion is in flight, for run-rule withdrawal of a further try.
/// </summary>
public sealed class OperatorSnapshotRetainedRun
{
    /// <summary>Trigger in effect before the daily pass replaced the run.</summary>
    public OperatorSnapshotCollectionTrigger? Trigger { get; set; }

    /// <summary>Terminal run copied when the first pass of the occasion starts.</summary>
    public OperatorSnapshotRun Run { get; set; } = new();
}
