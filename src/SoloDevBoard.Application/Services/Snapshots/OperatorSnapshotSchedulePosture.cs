namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Whether scheduled collection is modelled for this deployment posture.</summary>
public enum OperatorSnapshotSchedulePosture
{
    /// <summary>Lean posture has no schedule.</summary>
    Lean,

    /// <summary>Hosted sign-in models the daily schedule.</summary>
    Hosted,
}
