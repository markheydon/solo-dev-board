using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Web.Components.Features.Repositories;

/// <summary>Operator-facing copy for collection status on the Repositories page.</summary>
internal static class OperatorSnapshotStatusCopy
{
    internal static string FormatLocalTime(DateTimeOffset utc)
        => utc.ToLocalTime().ToString("g");

    internal static string BuildStatusMessage(OperatorSnapshotStatusDto status)
    {
        var schedule = "Nothing collects on a schedule.";
        var durability = status.DurabilityKind switch
        {
            OperatorSnapshotDurabilityKind.LocalMachine =>
                "The snapshot is kept on this machine rather than promised on another machine.",
            OperatorSnapshotDurabilityKind.EphemeralContainerDisk =>
                "The snapshot is kept on this container's disk. A restart can drop the snapshot.",
            OperatorSnapshotDurabilityKind.HostedBlob =>
                "The snapshot is kept for this signed-in operator and remains after a restart.",
            _ => string.Empty,
        };

        var state = status.Kind switch
        {
            OperatorSnapshotStatusKind.NeverRun =>
                "No snapshot is kept.",
            OperatorSnapshotStatusKind.InProgress when status.RunStartedAt is { } started =>
                $"Collection in progress. Started at {FormatLocalTime(started)}. Repositories recorded so far: {status.RepositoriesRecordedThisRun}.",
            OperatorSnapshotStatusKind.Succeeded when status.RunFinishedAt is { } finished =>
                $"Collection succeeded. Finished at {FormatLocalTime(finished)}. Repositories in the snapshot: {status.SnapshotRepositoryCount}.",
            OperatorSnapshotStatusKind.Failed when status.RunFinishedAt is { } failedAt =>
                BuildFailedMessage(status, failedAt),
            OperatorSnapshotStatusKind.Interrupted when status.RunFinishedAt is { } interruptedAt =>
                BuildInterruptedMessage(status, interruptedAt),
            _ => "No snapshot is kept.",
        };

        return $"{state} {schedule} {durability}".Trim();
    }

    private static string BuildFailedMessage(OperatorSnapshotStatusDto status, DateTimeOffset failedAt)
    {
        var reason = status.Reason switch
        {
            OperatorSnapshotRunReason.SignInOrToken => "a sign-in or token problem",
            OperatorSnapshotRunReason.RateLimit => "a GitHub rate limit",
            OperatorSnapshotRunReason.Unexpected => "an unexpected error",
            _ => "an unexpected error",
        };

        var detail = string.IsNullOrWhiteSpace(status.Detail) ? string.Empty : $" {status.Detail}";
        var message = $"Collection failed at {FormatLocalTime(failedAt)}. {CapitaliseFirst(reason)}.{detail}";
        return AppendLastSuccess(status, message);
    }

    private static string BuildInterruptedMessage(OperatorSnapshotStatusDto status, DateTimeOffset interruptedAt)
    {
        var message =
            $"Collection interrupted at {FormatLocalTime(interruptedAt)}. The site stopped before the collection finished.";
        return AppendLastSuccess(status, message);
    }

    private static string AppendLastSuccess(OperatorSnapshotStatusDto status, string message)
    {
        if (status.LastSuccessfulCollectionAt is not { } lastSuccess)
        {
            return message;
        }

        return
            $"{message} Last successful collection finished at {FormatLocalTime(lastSuccess)}. Repositories in that snapshot: {status.SnapshotRepositoryCount}.";
    }

    private static string CapitaliseFirst(string text)
        => string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text[1..];
}
