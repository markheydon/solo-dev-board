namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Schedule statement kind for operator-facing copy.</summary>
public enum OperatorSnapshotScheduleKind
{
    /// <summary>Names the next 05:00 UTC occasion.</summary>
    NextOccasion,

    /// <summary>Lean posture has no schedule.</summary>
    NoneLean,

    /// <summary>No schedule until a hosted sign-in completes.</summary>
    NoneUntilSignIn,

    /// <summary>Admission or hold excludes scheduled collection.</summary>
    NoneNotAllowed,
}
