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

        var scheduleClause = BuildScheduleClause(status);
        var runClause = status.Kind switch
        {
            OperatorSnapshotStatusKind.NeverRun => "No snapshot exists.",
            OperatorSnapshotStatusKind.InProgress when status.QueuedBeforeStart => scheduleClause,
            OperatorSnapshotStatusKind.InProgress when status.RunStartedAt is { } started =>
                BuildInProgressClause(status, started),
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

        if (string.IsNullOrWhiteSpace(scheduleClause) || runClause.Contains(scheduleClause, StringComparison.Ordinal))
        {
            return runClause;
        }

        if (string.IsNullOrWhiteSpace(runClause))
        {
            return scheduleClause;
        }

        return $"{runClause} {scheduleClause}";
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

        return status.Kind switch
        {
            OperatorSnapshotStatusKind.Failed => Severity.Error,
            OperatorSnapshotStatusKind.Interrupted => Severity.Warning,
            OperatorSnapshotStatusKind.InProgress => Severity.Info,
            OperatorSnapshotStatusKind.Succeeded => catalogueSeverity == Severity.Error ? catalogueSeverity : Severity.Success,
            _ => catalogueSeverity,
        };
    }

    private static string BuildInProgressClause(OperatorSnapshotStatusDto status, DateTimeOffset started)
    {
        if (status.StartedBy == OperatorSnapshotRunStartedBy.DailyOccasion)
        {
            return
                $"Saving snapshot from the daily collection at 05:00 UTC… started at {FormatLocalTime(started)}; {status.RepositoriesRecordedThisRun} recorded so far.";
        }

        return $"Saving snapshot… started at {FormatLocalTime(started)}; {status.RepositoriesRecordedThisRun} recorded so far.";
    }

    private static string BuildScheduleClause(OperatorSnapshotStatusDto status)
    {
        return status.Schedule switch
        {
            OperatorSnapshotScheduleKind.NoneLean =>
                "Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine.",
            OperatorSnapshotScheduleKind.NoneUntilSignIn =>
                "No scheduled collection will run until you sign in again.",
            OperatorSnapshotScheduleKind.NoneNotAllowed when status.Reason == OperatorSnapshotRunReason.SignInOrToken =>
                "No scheduled collection will run until you sign in again.",
            OperatorSnapshotScheduleKind.NoneNotAllowed =>
                "No scheduled collection will run because you are not allowed, your sign-in is too old, or stored access is waiting after a failure.",
            OperatorSnapshotScheduleKind.NextOccasion when status.NextOccasionAt is { } next =>
                BuildNextOccasionClause(status, next),
            _ => string.Empty,
        };
    }

    private static string BuildNextOccasionClause(OperatorSnapshotStatusDto status, DateTimeOffset nextOccasion)
    {
        var dayLabel = nextOccasion.UtcDateTime.Date == DateTime.UtcNow.Date ? "today" : "the following day";
        if (status.QueuedBeforeStart)
        {
            return $"The next scheduled collection is {dayLabel} at 05:00 UTC.";
        }

        return $"The next scheduled collection is {dayLabel} at 05:00 UTC.";
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
        var message =
            $"Snapshot was not finished at {FormatLocalTime(interruptedAt)} because the site stopped before it completed.";
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
