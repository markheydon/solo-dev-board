using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <inheritdoc />
public sealed class OperatorSnapshotScheduleFactsService(
    IScheduledCollectionEnvironment environment,
    HostedCollectionEligibilityEvaluator eligibility) : IOperatorSnapshotScheduleFactsService
{
    private readonly IScheduledCollectionEnvironment _environment =
        environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly HostedCollectionEligibilityEvaluator _eligibility =
        eligibility ?? throw new ArgumentNullException(nameof(eligibility));

    /// <inheritdoc/>
    public OperatorSnapshotScheduleFacts Compute(
        OperatorSnapshotDocument document,
        HostedSignInRecordForSchedule? signInRecord,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_environment.IsHostedScheduleEnabled)
        {
            return new OperatorSnapshotScheduleFacts(
                OperatorSnapshotSchedulePosture.Lean,
                OperatorSnapshotNoScheduleReason.Lean,
                null,
                null,
                document.Trigger);
        }

        var trigger = document.Trigger;
        var queued = document.QueuedOccasion;
        var todayOccasion = GetTodayOccasionAt(utcNow);
        var todayDate = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var noSchedule = OperatorSnapshotNoScheduleReason.None;
        DateTimeOffset? nextOccasion = null;

        if (signInRecord is null)
        {
            noSchedule = OperatorSnapshotNoScheduleReason.OutsideWindow;
        }
        else
        {
            var scheduleRecord = new HostedSignInRecord
            {
                OperatorLogin = document.OperatorLogin,
                CompletedAt = signInRecord.CompletedAt,
                CredentialUnusable = signInRecord.CredentialUnusable,
                OrganisationLogins = signInRecord.OrganisationLogins,
            };

            var nameInstant = utcNow < todayOccasion ? todayOccasion : todayOccasion.AddDays(1);

            if (!_eligibility.IsAdmissionAllowed(scheduleRecord))
            {
                noSchedule = OperatorSnapshotNoScheduleReason.NotAllowed;
            }
            else if (signInRecord.CredentialUnusable)
            {
                noSchedule = OperatorSnapshotNoScheduleReason.CredentialUnusable;
            }
            else if (!_eligibility.WouldBeEligibleForSchedule(scheduleRecord, nameInstant))
            {
                noSchedule = OperatorSnapshotNoScheduleReason.OutsideWindow;
            }
            else if (queued is not null && string.Equals(queued, todayDate, StringComparison.Ordinal))
            {
                nextOccasion = todayOccasion;
            }
            else if (utcNow < todayOccasion)
            {
                nextOccasion = todayOccasion;
            }
            else
            {
                nextOccasion = todayOccasion.AddDays(1);
            }
        }

        if (noSchedule != OperatorSnapshotNoScheduleReason.None)
        {
            nextOccasion = null;
        }

        return new OperatorSnapshotScheduleFacts(
            OperatorSnapshotSchedulePosture.Hosted,
            noSchedule,
            queued,
            nextOccasion,
            trigger);
    }

    private static DateTimeOffset GetTodayOccasionAt(DateTimeOffset utcNow)
    {
        var date = utcNow.UtcDateTime.Date;
        return new DateTimeOffset(date.Year, date.Month, date.Day, 5, 0, 0, TimeSpan.Zero);
    }
}
