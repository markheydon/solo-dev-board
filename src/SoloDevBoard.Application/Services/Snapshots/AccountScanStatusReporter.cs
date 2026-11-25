namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Derives reported scan status from stored blobs without writing.</summary>
public sealed class AccountScanStatusReporter(TimeProvider timeProvider)
{
    private static readonly TimeSpan StalledThreshold = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Reports status for a stored document.</summary>
    public ReportedAccountScanStatus Report(AccountScanStatusDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ReportStoredStatus(document.Status, document.LivenessAt, document.RunningSince, _timeProvider.GetUtcNow());
    }

    /// <summary>Reports when sign-in exists but the blob is absent.</summary>
    public ReportedAccountScanStatus ReportMissingBlob() => ReportedAccountScanStatus.NeverRun;

    /// <summary>Maps stored JSON status to reported status at an instant.</summary>
    internal static ReportedAccountScanStatus ReportStoredStatus(
        string storedStatus,
        DateTimeOffset? livenessAt,
        DateTimeOffset? runningSince,
        DateTimeOffset utcNow)
    {
        if (string.Equals(storedStatus, AccountScanStatusDocumentConstants.StatusNeverRun, StringComparison.Ordinal))
        {
            return ReportedAccountScanStatus.NeverRun;
        }

        if (string.Equals(storedStatus, AccountScanStatusDocumentConstants.StatusScheduled, StringComparison.Ordinal))
        {
            return ReportedAccountScanStatus.Scheduled;
        }

        if (string.Equals(storedStatus, AccountScanStatusDocumentConstants.StatusFinished, StringComparison.Ordinal))
        {
            return ReportedAccountScanStatus.Finished;
        }

        if (string.Equals(storedStatus, AccountScanStatusDocumentConstants.StatusSuspended, StringComparison.Ordinal))
        {
            return ReportedAccountScanStatus.Suspended;
        }

        if (!string.Equals(storedStatus, AccountScanStatusDocumentConstants.StatusRunning, StringComparison.Ordinal))
        {
            return ReportedAccountScanStatus.NeverRun;
        }

        var mark = livenessAt ?? runningSince;
        if (mark is null)
        {
            return ReportedAccountScanStatus.Running;
        }

        var age = utcNow - mark.Value;
        return age > StalledThreshold
            ? ReportedAccountScanStatus.Stalled
            : ReportedAccountScanStatus.Running;
    }

    /// <summary>Collection trigger fact for the page.</summary>
    public static OperatorSnapshotCollectionTrigger? ResolveTriggerFact(ReportedAccountScanStatus reported) =>
        reported is ReportedAccountScanStatus.Running or ReportedAccountScanStatus.Stalled or ReportedAccountScanStatus.Finished
            ? null // caller sets from document context when running/finished
            : null;
}
