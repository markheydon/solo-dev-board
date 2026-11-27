using System.Globalization;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>FR-025 status and schedule sentences for hosted sign-in.</summary>
public static class AccountScanStatusSentenceFormatter
{
    private static readonly CultureInfo UnitedKingdomCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>Builds hosted status and schedule sentences.</summary>
    public static (string? StatusSentence, string? ScheduleSentence) Format(
        OperatorSnapshotSchedulePosture posture,
        OperatorSnapshotNoScheduleReason noScheduleReason,
        ReportedAccountScanStatus? reported,
        AccountScanStatusReport? scanReport,
        OperatorSnapshotStatusDto runtime,
        HostedSignInRecordForSchedule? signIn,
        bool nextAskIncludes,
        bool workerOffered)
    {
        if (posture == OperatorSnapshotSchedulePosture.Lean)
        {
            return (null, null);
        }

        var hasWorker = signIn?.HasWorkerRefreshToken ?? false;
        var credentialWait = signIn?.CredentialUnusable ?? false;
        var schedule = BuildScheduleSentence(noScheduleReason, reported, hasWorker, credentialWait, workerOffered, nextAskIncludes);
        var status = reported is null ? null : BuildStatusSentence(reported.Value, scanReport, runtime);

        return (status, schedule);
    }

    private static string? BuildScheduleSentence(
        OperatorSnapshotNoScheduleReason noScheduleReason,
        ReportedAccountScanStatus? reported,
        bool hasWorker,
        bool credentialWait,
        bool workerOffered,
        bool nextAskIncludes)
    {
        if (noScheduleReason == OperatorSnapshotNoScheduleReason.NotAllowed)
        {
            return "No scheduled collection will run because you are not allowed to use the product.";
        }

        if (noScheduleReason == OperatorSnapshotNoScheduleReason.CredentialUnusable)
        {
            return "No scheduled collection will run until you refresh the access used for the overnight collection.";
        }

        if (noScheduleReason == OperatorSnapshotNoScheduleReason.OutsideWindow)
        {
            return "No scheduled collection will run until you sign in again.";
        }

        if (nextAskIncludes || reported is ReportedAccountScanStatus.Scheduled)
        {
            return "The next collection runs overnight.";
        }

        return workerOffered && !credentialWait && hasWorker == false
            ? "The next collection runs overnight."
            : null;
    }

    private static string BuildStatusSentence(
        ReportedAccountScanStatus reported,
        AccountScanStatusReport? scanReport,
        OperatorSnapshotStatusDto runtime)
    {
        var lastSuccessSuffix = runtime.LastSuccessfulCollectionAt is { } lastSuccess
            ? $" The last successful snapshot was saved at {FormatLocalTime(lastSuccess)}."
            : string.Empty;

        return reported switch
        {
            ReportedAccountScanStatus.NeverRun =>
                $"No collection has run yet.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Scheduled =>
                $"A collection is waiting to start.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Running when runtime.RunStartedAt is { } started =>
                $"A collection is running. It started at {FormatLocalTime(started)}. {runtime.RepositoriesRecordedThisRun} repositories recorded so far.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Running =>
                $"A collection is running.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Stalled =>
                $"This collection has stopped reporting progress.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Finished when string.Equals(
                scanReport?.LastResult,
                AccountScanStatusDocumentConstants.LastResultSuccess,
                StringComparison.Ordinal) =>
                BuildFinishedSuccessSentence(scanReport, runtime, lastSuccessSuffix),

            ReportedAccountScanStatus.Finished =>
                BuildFinishedFailureSentence(scanReport, lastSuccessSuffix),

            ReportedAccountScanStatus.Suspended when scanReport?.SuspensionReason == AccountScanSuspensionReason.NotAllowed =>
                $"Scheduled collection is paused because you are not allowed to use the product.{lastSuccessSuffix}".TrimEnd(),

            ReportedAccountScanStatus.Suspended =>
                BuildSuspendedTokenSentence(scanReport, lastSuccessSuffix),

            _ => string.Empty,
        };
    }

    private static string BuildFinishedSuccessSentence(
        AccountScanStatusReport? scanReport,
        OperatorSnapshotStatusDto runtime,
        string lastSuccessSuffix)
    {
        var finish = scanReport?.LastScanAt is { } at ? FormatLocalTime(at) : "unknown time";
        var count = runtime.SnapshotRepositoryCount;
        return $"The last collection finished successfully at {finish}. {count} repositories were saved.";
    }

    private static string BuildFinishedFailureSentence(AccountScanStatusReport? scanReport, string lastSuccessSuffix)
    {
        var failedAt = scanReport?.LastScanAt is { } at ? FormatLocalTime(at) : "unknown time";
        var comment = FormatFailureComment(scanReport?.ResultComment);
        return $"The last collection finished at {failedAt} without saving a snapshot because of {comment}.{lastSuccessSuffix}".TrimEnd();
    }

    private static string BuildSuspendedTokenSentence(AccountScanStatusReport? scanReport, string lastSuccessSuffix)
    {
        var failedAt = scanReport?.LastScanAt is { } at ? FormatLocalTime(at) : "unknown time";
        return $"Scheduled collection is paused until you refresh the access used for the overnight collection. The collection at {failedAt} did not save a snapshot because of a sign-in or token problem.{lastSuccessSuffix}".TrimEnd();
    }

    private static string FormatFailureComment(string? comment) => comment switch
    {
        null or "" => "a sign-in or token problem",
        var c when c.Contains("rate", StringComparison.OrdinalIgnoreCase) => "a GitHub rate limit",
        var c when c.Contains("unexpected", StringComparison.OrdinalIgnoreCase) => "an unexpected error",
        _ => "a sign-in or token problem",
    };

    private static string FormatLocalTime(DateTimeOffset utc) =>
        utc.ToLocalTime().ToString("g", UnitedKingdomCulture);

}
