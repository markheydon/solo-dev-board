namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Outcome of starting a collection.</summary>
public enum OperatorSnapshotStartResult
{
    /// <summary>Walk accepted.</summary>
    Started,

    /// <summary>Another walk is already in progress for this operator.</summary>
    AlreadyInProgress,
}
