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

        var occasionInstant = GetOccasionInstantForFacts(utcNow);
        var nextOccasion = default(DateTimeOffset?);
        var noSchedule = OperatorSnapshotNoScheduleReason.None;

        if (signInRecord is null)
        {
            noSchedule = OperatorSnapshotNoScheduleReason.OutsideWindow;
        }
        else if (signInRecord.CredentialUnusable)
        {
            noSchedule = OperatorSnapshotNoScheduleReason.CredentialUnusable;
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
            if (!_eligibility.WouldBeEligibleForSchedule(scheduleRecord, occasionInstant))
            {
                noSchedule = !IsInsideSignInWindow(signInRecord.CompletedAt, occasionInstant)
                    ? OperatorSnapshotNoScheduleReason.OutsideWindow
                    : OperatorSnapshotNoScheduleReason.NotAllowed;
            }
            else if (TryComputeNextOccasion(utcNow, signInRecord, occasionInstant, out var next))
            {
                nextOccasion = next;
            }
            else
            {
                noSchedule = OperatorSnapshotNoScheduleReason.OutsideWindow;
            }
        }

        if (noSchedule == OperatorSnapshotNoScheduleReason.None && nextOccasion is null
            && TryComputeNextOccasion(utcNow, signInRecord!, occasionInstant, out var computed))
        {
            nextOccasion = computed;
        }
        if (queued is not null && noSchedule == OperatorSnapshotNoScheduleReason.None)
        {
            var today = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (string.Equals(queued, today, StringComparison.Ordinal))
            {
                nextOccasion = GetTodayOccasionAt(utcNow);
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

    private static bool TryComputeNextOccasion(
        DateTimeOffset utcNow,
        HostedSignInRecordForSchedule signInRecord,
        DateTimeOffset occasionInstant,
        out DateTimeOffset nextOccasion)
    {
        var todayOccasion = GetTodayOccasionAt(utcNow);
        if (utcNow <= todayOccasion.AddMinutes(15)
            && signInRecord.CompletedAt <= todayOccasion
            && IsInsideSignInWindow(signInRecord.CompletedAt, utcNow <= todayOccasion ? todayOccasion : utcNow))
        {
            nextOccasion = todayOccasion;
            return true;
        }

        var tomorrowOccasion = todayOccasion.AddDays(1);
        if (IsInsideSignInWindow(signInRecord.CompletedAt, tomorrowOccasion))
        {
            nextOccasion = tomorrowOccasion;
            return true;
        }

        nextOccasion = default;
        return false;
    }

    private static DateTimeOffset GetTodayOccasionAt(DateTimeOffset utcNow)
    {
        var date = utcNow.UtcDateTime.Date;
        return new DateTimeOffset(date.Year, date.Month, date.Day, 5, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset GetOccasionInstantForFacts(DateTimeOffset utcNow)
    {
        var todayOccasion = GetTodayOccasionAt(utcNow);
        if (utcNow <= todayOccasion.AddMinutes(15))
        {
            return utcNow > todayOccasion ? utcNow : todayOccasion;
        }

        return todayOccasion.AddDays(1);
    }

    private static bool IsInsideSignInWindow(DateTimeOffset completedAt, DateTimeOffset occasionInstant)
        => completedAt >= occasionInstant.AddDays(-30);
}
