namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Operator-visible collection status.</summary>
public enum OperatorSnapshotStatusKind
{
    /// <summary>No snapshot document exists.</summary>
    NeverRun,

    /// <summary>Collection is in progress.</summary>
    InProgress,

    /// <summary>Last run succeeded.</summary>
    Succeeded,

    /// <summary>Last run failed.</summary>
    Failed,

    /// <summary>Last run was interrupted.</summary>
    Interrupted,
}
