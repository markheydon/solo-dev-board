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
            _ when status.QueuedForOccasion is not null && status.Kind != OperatorSnapshotStatusKind.InProgress =>
                "Next scheduled collection: 05:00 UTC.",
            _ when status.NextOccasionAtUtc is { } next && next.UtcDateTime.TimeOfDay == TimeSpan.FromHours(5) =>
                next.UtcDateTime.Date == DateTime.UtcNow.Date
                    ? "Next scheduled collection: 05:00 UTC."
                    : "Next scheduled collection: 05:00 UTC on the following UTC day.",
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

        var schedule = BuildScheduleClause(status);

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
