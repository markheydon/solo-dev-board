namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Values for <c>run.status</c> in the operator snapshot document.</summary>
public enum OperatorSnapshotRunStatus
{
    /// <summary>Collection is still running.</summary>
    InProgress,

    /// <summary>Collection succeeded.</summary>
    Succeeded,

    /// <summary>Collection failed.</summary>
    Failed,

    /// <summary>Collection was interrupted because the site stopped.</summary>
    Interrupted,
}
