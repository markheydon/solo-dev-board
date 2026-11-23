namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Computes schedule-related status facts for the Repositories page.</summary>
public interface IOperatorSnapshotScheduleFactsService
{
    /// <summary>Returns schedule facts for the snapshot document and optional sign-in record.</summary>
    OperatorSnapshotScheduleFacts Compute(
        OperatorSnapshotDocument document,
        HostedSignInRecordForSchedule? signInRecord,
        DateTimeOffset utcNow);
}

/// <summary>Sign-in fields needed for schedule fact computation.</summary>
public sealed record HostedSignInRecordForSchedule(
    string OperatorLogin,
    DateTimeOffset CompletedAt,
    bool CredentialUnusable,
    IReadOnlyList<string> OrganisationLogins);

/// <summary>Schedule fields merged into <see cref="OperatorSnapshotStatusDto"/>.</summary>
public sealed record OperatorSnapshotScheduleFacts(
    OperatorSnapshotSchedulePosture SchedulePosture,
    OperatorSnapshotNoScheduleReason NoScheduleReason,
    string? QueuedForOccasion,
    DateTimeOffset? NextOccasionAtUtc,
    OperatorSnapshotCollectionTrigger? Trigger);
