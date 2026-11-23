namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Why the next 05:00 UTC occasion will not queue this operator.</summary>
public enum OperatorSnapshotNoScheduleReason
{
    /// <summary>Eligible; no blocking reason.</summary>
    None,

    /// <summary>Lean posture has no schedule.</summary>
    Lean,

    /// <summary>Daily pass failed when stored access could not be used.</summary>
    CredentialUnusable,

    /// <summary>Latest hosted sign-in is outside the 30-day window.</summary>
    OutsideWindow,

    /// <summary>Hosted admission lists refuse this operator at fill time.</summary>
    NotAllowed,
}
