using System.Globalization;
using MudBlazor;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Web.Components.Features.Repositories;

/// <summary>Operator-facing copy for collection status on the Repositories page.</summary>
internal static class OperatorSnapshotStatusCopy
{
    private static readonly CultureInfo UnitedKingdomCulture = CultureInfo.GetCultureInfo("en-GB");

    internal static string FormatLocalTime(DateTimeOffset utc)
        => utc.ToLocalTime().ToString("g", UnitedKingdomCulture);

    /// <summary>Fixed comment shown when the account is suspended because it is not allowed.</summary>
    internal const string NotAllowedComment = "You are not allowed to use the product.";

    /// <summary>True while a scan is working (hosted: told running; lean: the file run is in progress).</summary>
    internal static bool IsScanWorking(OperatorSnapshotStatusDto? status) =>
        status is not null
        && (status.ReportedScanStatus is { } reported
            ? reported == ReportedAccountScanStatus.Running
            : status.Kind == OperatorSnapshotStatusKind.InProgress);

    /// <summary>True when Collect snapshot must stay disabled (hosted: told scheduled or running).</summary>
    internal static bool IsCollectBlocked(OperatorSnapshotStatusDto? status) =>
        status is not null
        && (status.ReportedScanStatus is { } reported
            ? reported is ReportedAccountScanStatus.Scheduled or ReportedAccountScanStatus.Running
            : status.Kind == OperatorSnapshotStatusKind.InProgress);

    /// <summary>Schedule sentence for the status alert.</summary>
    internal static string? BuildScheduleClause(OperatorSnapshotStatusDto? status)
    {
        if (status is null)
        {
            return null;
        }

        if (status.SchedulePosture == OperatorSnapshotSchedulePosture.Lean)
        {
            return "Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine.";
        }

        return status.NoScheduleReason switch
        {
            OperatorSnapshotNoScheduleReason.CredentialUnusable =>
                "No scheduled collection will run until you sign in again.",
            OperatorSnapshotNoScheduleReason.OutsideWindow =>
                "No scheduled collection will run until you sign in again.",
            OperatorSnapshotNoScheduleReason.NotAllowed =>
                "No scheduled collection will run because you are not allowed to use the product.",
            _ when status.NextOccasionAtUtc is { } next && next.UtcDateTime.TimeOfDay == TimeSpan.FromHours(5) =>
                status.NextOccasionOnFollowingUtcDay
                    ? "Next scheduled collection: 05:00 UTC on the following UTC day."
                    : "Next scheduled collection: 05:00 UTC.",
            _ => null,
        };
    }

    /// <summary>Short clause appended to the catalogue feedback line (same alert).</summary>
    internal static string BuildSnapshotSuffix(OperatorSnapshotStatusDto? status, bool statusUnavailable)
    {
        if (statusUnavailable)
        {
            return "Snapshot status is unavailable right now.";
        }

        if (status is null)
        {
            return string.Empty;
        }

        if (status.SchedulePosture == OperatorSnapshotSchedulePosture.Hosted
            && (!string.IsNullOrEmpty(status.StatusSentence) || !string.IsNullOrEmpty(status.ScheduleSentence)))
        {
            return Join(status.ScheduleSentence, status.StatusSentence ?? string.Empty);
        }

        var schedule = BuildScheduleClause(status);

        if (status.ReportedScanStatus is { } reported)
        {
            return Join(schedule, BuildHostedStatusClause(status, reported));
        }

        var runClause = status.Kind switch
        {
            OperatorSnapshotStatusKind.NeverRun => "No snapshot exists.",
            OperatorSnapshotStatusKind.InProgress when status.RunStartedAt is { } started =>
                BuildInProgressSuffix(status, started),
            OperatorSnapshotStatusKind.Succeeded when status.LastSuccessfulCollectionAt is { } finished =>
                $"Snapshot saved at {FormatLocalTime(finished)} ({status.SnapshotRepositoryCount} repositories included).",
            OperatorSnapshotStatusKind.Succeeded when status.RunFinishedAt is { } finished =>
                $"Snapshot saved at {FormatLocalTime(finished)} ({status.SnapshotRepositoryCount} repositories included).",
            OperatorSnapshotStatusKind.Failed when status.RunFinishedAt is { } failedAt =>
                BuildFailedSuffix(status, failedAt),
            OperatorSnapshotStatusKind.Interrupted when status.RunFinishedAt is { } interruptedAt =>
                BuildInterruptedSuffix(status, interruptedAt),
            _ => string.Empty,
        };

        return Join(schedule, runClause);
    }

    private static string Join(string? schedule, string runClause)
    {
        if (string.IsNullOrEmpty(schedule))
        {
            return runClause;
        }

        if (string.IsNullOrEmpty(runClause))
        {
            return schedule;
        }

        return $"{schedule} {runClause}";
    }

    private static string BuildHostedStatusClause(OperatorSnapshotStatusDto status, ReportedAccountScanStatus reported)
    {
        switch (reported)
        {
            case ReportedAccountScanStatus.NeverRun:
                return "Never run.";

            case ReportedAccountScanStatus.Scheduled:
                return AppendLastSuccessSuffix(status, "Scheduled.");

            case ReportedAccountScanStatus.Running:
                var trigger = status.Trigger == OperatorSnapshotCollectionTrigger.Daily
                    ? "The daily occasion started it."
                    : "You started it.";
                return status.RunStartedAt is { } started
                    ? $"Running. {BuildInProgressSuffix(status, started)}"
                    : $"Running. {trigger}";

            case ReportedAccountScanStatus.Stalled:
                return AppendLastSuccessSuffix(
                    status,
                    "Stalled. The scan has not reported progress for more than five minutes. "
                    + "Collect snapshot is still available.");

            case ReportedAccountScanStatus.Finished:
                return BuildHostedFinishedClause(status);

            case ReportedAccountScanStatus.Suspended:
                return BuildHostedSuspendedClause(status);

            default:
                return string.Empty;
        }
    }

    private static string BuildHostedFinishedClause(OperatorSnapshotStatusDto status)
    {
        var at = status.LastScanAt is { } scanAt ? FormatLocalTime(scanAt) : null;
        if (string.Equals(status.LastScanResult, AccountScanStatusDocumentConstants.LastResultFailure, StringComparison.Ordinal))
        {
            var failed = at is null ? "Snapshot could not be saved." : $"Snapshot could not be saved at {at}.";
            return AppendLastSuccessSuffix(status, $"Finished. {failed} {status.ResultComment}".TrimEnd());
        }

        var saved = at is null
            ? $"Snapshot saved ({status.SnapshotRepositoryCount} repositories included)."
            : $"Snapshot saved at {at} ({status.SnapshotRepositoryCount} repositories included).";
        return $"Finished. {status.ResultComment} {saved}".Replace("  ", " ", StringComparison.Ordinal);
    }

    private static string BuildHostedSuspendedClause(OperatorSnapshotStatusDto status)
    {
        if (status.ScanSuspensionReason == AccountScanSuspensionReason.NotAllowed)
        {
            var message = $"Suspended. {NotAllowedComment}";
            if (status.LastScanAt is { } previous
                && !string.IsNullOrEmpty(status.LastScanResult))
            {
                message += $" Last scan: {status.LastScanResult} at {FormatLocalTime(previous)}.";
            }

            return AppendLastSuccessSuffix(status, message);
        }

        var at = status.LastScanAt is { } scanAt ? FormatLocalTime(scanAt) : null;
        var failed = at is null ? "Snapshot could not be saved." : $"Snapshot could not be saved at {at}.";
        return AppendLastSuccessSuffix(status, $"Suspended. {failed} {status.ResultComment}".TrimEnd());
    }

    internal static Severity ResolveCombinedSeverity(
        OperatorSnapshotStatusDto? status,
        bool statusUnavailable,
        Severity catalogueSeverity)
    {
        if (statusUnavailable)
        {
            return Severity.Warning;
        }

        if (status is null)
        {
            return catalogueSeverity;
        }

        if (status.ReportedScanStatus is { } reported)
        {
            return reported switch
            {
                ReportedAccountScanStatus.Finished when string.Equals(
                    status.LastScanResult,
                    AccountScanStatusDocumentConstants.LastResultFailure,
                    StringComparison.Ordinal) => Severity.Error,
                ReportedAccountScanStatus.Suspended when status.ScanSuspensionReason == AccountScanSuspensionReason.None
                    => Severity.Error,
                ReportedAccountScanStatus.Suspended or ReportedAccountScanStatus.Stalled => Severity.Warning,
                ReportedAccountScanStatus.Running or ReportedAccountScanStatus.Scheduled => Severity.Info,
                ReportedAccountScanStatus.Finished => catalogueSeverity == Severity.Error ? catalogueSeverity : Severity.Success,
                _ => catalogueSeverity,
            };
        }

        return status.Kind switch
        {
            OperatorSnapshotStatusKind.Failed => Severity.Error,
            OperatorSnapshotStatusKind.Interrupted => Severity.Warning,
            OperatorSnapshotStatusKind.InProgress => Severity.Info,
            OperatorSnapshotStatusKind.Succeeded => catalogueSeverity == Severity.Error ? catalogueSeverity : Severity.Success,
            _ => catalogueSeverity,
        };
    }

    private static string BuildInProgressSuffix(OperatorSnapshotStatusDto status, DateTimeOffset started)
    {
        var trigger = status.Trigger == OperatorSnapshotCollectionTrigger.Daily
            ? "The daily occasion started it."
            : "You started it.";
        return
            $"{trigger} Saving snapshot… started at {FormatLocalTime(started)}; {status.RepositoriesRecordedThisRun} recorded so far.";
    }

    private static string BuildFailedSuffix(OperatorSnapshotStatusDto status, DateTimeOffset failedAt)
    {
        var reason = status.Reason switch
        {
            OperatorSnapshotRunReason.SignInOrToken => "Could not reach GitHub with your current sign-in.",
            OperatorSnapshotRunReason.RateLimit => "GitHub rate limit reached.",
            OperatorSnapshotRunReason.Unexpected => "Something went wrong.",
            _ => "Something went wrong.",
        };

        var detail = string.IsNullOrWhiteSpace(status.Detail) ? string.Empty : $" {status.Detail}";
        var message = $"Snapshot could not be saved at {FormatLocalTime(failedAt)}. {reason}{detail}";
        return AppendLastSuccessSuffix(status, message);
    }

    private static string BuildInterruptedSuffix(OperatorSnapshotStatusDto status, DateTimeOffset interruptedAt)
    {
        var message = status.Reason == OperatorSnapshotRunReason.ScheduledCollectionStopped
            ? $"Snapshot was not finished at {FormatLocalTime(interruptedAt)} because the scheduled collection stopped before it finished."
            : $"Snapshot was not finished at {FormatLocalTime(interruptedAt)} because the site stopped before it completed.";
        return AppendLastSuccessSuffix(status, message);
    }

    private static string AppendLastSuccessSuffix(OperatorSnapshotStatusDto status, string message)
    {
        if (status.LastSuccessfulCollectionAt is not { } lastSuccess)
        {
            return message;
        }

        return
            $"{message} Last successful snapshot: {FormatLocalTime(lastSuccess)} ({status.SnapshotRepositoryCount} repositories).";
    }
}
