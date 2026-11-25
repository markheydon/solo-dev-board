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
        ReportedAccountScanStatus? reportedScanStatus,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_environment.IsHostedScheduleEnabled)
        {
            return new OperatorSnapshotScheduleFacts(
                OperatorSnapshotSchedulePosture.Lean,
                OperatorSnapshotNoScheduleReason.Lean,
                null,
                document.Trigger);
        }

        var trigger = document.Trigger;
        var todayOccasion = GetTodayOccasionAt(utcNow);
        var scheduled = reportedScanStatus == ReportedAccountScanStatus.Scheduled;

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
                OperatorLogin = signInRecord.OperatorLogin,
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
            else if (scheduled)
            {
                // A queued wait stays today; the 30-day window does not clear it.
                nextOccasion = todayOccasion;
            }
            else if (!_eligibility.WouldBeEligibleForSchedule(scheduleRecord, nameInstant))
            {
                noSchedule = OperatorSnapshotNoScheduleReason.OutsideWindow;
            }
            else
            {
                nextOccasion = nameInstant;
            }
        }

        if (noSchedule != OperatorSnapshotNoScheduleReason.None)
        {
            nextOccasion = null;
        }

        return new OperatorSnapshotScheduleFacts(
            OperatorSnapshotSchedulePosture.Hosted,
            noSchedule,
            nextOccasion,
            trigger);
    }

    private static DateTimeOffset GetTodayOccasionAt(DateTimeOffset utcNow)
    {
        var date = utcNow.UtcDateTime.Date;
        return new DateTimeOffset(date.Year, date.Month, date.Day, 5, 0, 0, TimeSpan.Zero);
    }
}
