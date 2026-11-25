namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Status told to callers (includes computed <see cref="Stalled"/>).</summary>
public enum ReportedAccountScanStatus
{
    /// <summary>No scan has run (or blob absent with sign-in).</summary>
    NeverRun,

    /// <summary>Queued, including invisible retries.</summary>
    Scheduled,

    /// <summary>Scan in progress with a fresh liveness mark.</summary>
    Running,

    /// <summary>Running but liveness mark is strictly older than five minutes.</summary>
    Stalled,

    /// <summary>Terminal success or failure.</summary>
    Finished,

    /// <summary>Blocked until not-allowed or credential issue is resolved.</summary>
    Suspended,
}

/// <summary>Maps stored JSON status to reported words for UI.</summary>
public static class ReportedAccountScanStatusFormatting
{
    /// <summary>Lowercase words used on the Repositories page.</summary>
    public static string ToDisplayWord(ReportedAccountScanStatus status) => status switch
    {
        ReportedAccountScanStatus.NeverRun => "never run",
        ReportedAccountScanStatus.Scheduled => "scheduled",
        ReportedAccountScanStatus.Running => "running",
        ReportedAccountScanStatus.Stalled => "stalled",
        ReportedAccountScanStatus.Finished => "finished",
        ReportedAccountScanStatus.Suspended => "suspended",
        _ => "never run",
    };
}
