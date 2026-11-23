using Microsoft.Extensions.Options;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Builds schedule fields for <see cref="OperatorSnapshotStatusDto"/>.</summary>
public sealed class OperatorSnapshotScheduleStatusBuilder(
    IOperatorSignInRecordStore signInStore,
    IDailyOccasionQueueStore queueStore,
    OperatorSnapshotEligibility eligibility,
    IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
    TimeProvider timeProvider)
{
    private readonly IOperatorSignInRecordStore _signInStore = signInStore ?? throw new ArgumentNullException(nameof(signInStore));
    private readonly IDailyOccasionQueueStore _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
    private readonly OperatorSnapshotEligibility _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
    private readonly OperatorSnapshotDurabilityOptions _durability = durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Resolves schedule metadata for the signed-in operator.</summary>
    public async Task<(OperatorSnapshotScheduleKind Schedule, DateTimeOffset? NextOccasionAt, bool QueuedBeforeStart, OperatorSnapshotRunStartedBy? StartedBy)> BuildAsync(
        string sanitisedLogin,
        OperatorSnapshotDocument? document,
        CancellationToken cancellationToken)
    {
        if (_durability.DurabilityKind != OperatorSnapshotDurabilityKind.HostedBlob)
        {
            return (OperatorSnapshotScheduleKind.NoneLean, null, false, document?.Run.StartedBy);
        }

        var record = await _signInStore.GetByLoginAsync(sanitisedLogin, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return (OperatorSnapshotScheduleKind.NoneUntilSignIn, null, false, document?.Run.StartedBy);
        }

        var queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var entry = queue?.FindEntry(sanitisedLogin);
        var queuedBeforeStart = entry?.State == QueueEntryState.Waiting;

        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var todayOccasion = OperatorSnapshotEligibility.OccasionInstant(today);
        var nextOccasion = todayOccasion;
        if (now > todayOccasion.AddMinutes(IDailyCollectionScheduler.Constants.FillGraceMinuteUtc))
        {
            nextOccasion = OperatorSnapshotEligibility.OccasionInstant(today.AddDays(1));
        }

        if (record.ScheduleHold)
        {
            return (OperatorSnapshotScheduleKind.NoneNotAllowed, null, queuedBeforeStart, document?.Run.StartedBy);
        }

        var windowStart = nextOccasion - IDailyCollectionScheduler.Constants.SignInWindow;
        if (record.CompletedAt < windowStart || record.CompletedAt > nextOccasion)
        {
            return (OperatorSnapshotScheduleKind.NoneNotAllowed, null, queuedBeforeStart, document?.Run.StartedBy);
        }

        if (!_eligibility.IsAdmissionAllowed(record))
        {
            return (OperatorSnapshotScheduleKind.NoneNotAllowed, null, queuedBeforeStart, document?.Run.StartedBy);
        }

        var inProgress = document?.Run.Status == OperatorSnapshotRunStatus.InProgress;
        var startedBy = inProgress ? document?.Run.StartedBy : document?.Run.StartedBy;

        if (queuedBeforeStart)
        {
            return (OperatorSnapshotScheduleKind.NextOccasion, todayOccasion, true, startedBy);
        }

        if (_eligibility.IsEligibleAtFill(record, nextOccasion, queue, document))
        {
            return (OperatorSnapshotScheduleKind.NextOccasion, nextOccasion, false, startedBy);
        }

        return (OperatorSnapshotScheduleKind.NoneNotAllowed, null, false, startedBy);
    }
}
