using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Pure eligibility rules at a fill instant.</summary>
public sealed class OperatorSnapshotEligibility
{
    private readonly HostedAdmissionControlOptions _admissionOptions;

    /// <summary>Initialises eligibility rules.</summary>
    public OperatorSnapshotEligibility(IOptions<HostedAdmissionControlOptions> admissionOptions)
    {
        _admissionOptions = admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));
    }

    /// <summary>Decides whether a sign-in record may be queued at the occasion instant.</summary>
    public bool IsEligibleAtFill(
        OperatorSignInRecord record,
        DateTimeOffset occasionInstant,
        DailyOccasionQueue? queue,
        OperatorSnapshotDocument? snapshot)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.ScheduleHold)
        {
            return false;
        }

        var windowStart = occasionInstant - IDailyCollectionScheduler.Constants.SignInWindow;
        if (record.CompletedAt < windowStart)
        {
            return false;
        }

        if (record.CompletedAt > occasionInstant)
        {
            return false;
        }

        if (queue is not null)
        {
            if (queue.FindEntry(record.OperatorLogin) is not null)
            {
                return false;
            }

            if (string.Equals(queue.ActiveLogin, record.OperatorLogin, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (snapshot?.Run.Status == OperatorSnapshotRunStatus.InProgress)
        {
            return false;
        }

        return IsAdmissionAllowed(record);
    }

    /// <summary>Decides whether admission would allow a record at the fill instant.</summary>
    public bool IsAdmissionAllowed(OperatorSignInRecord record)
    {
        if (!_admissionOptions.Enabled)
        {
            return true;
        }

        var allowedUsers = HostedAdmissionAllowList.BuildNormalisedSet(_admissionOptions.AllowedUserLogins);
        var allowedOrganisations = HostedAdmissionAllowList.BuildNormalisedSet(_admissionOptions.AllowedOrganisationLogins);

        if (allowedUsers.Count == 0 && allowedOrganisations.Count == 0)
        {
            return false;
        }

        if (allowedUsers.Contains(record.OperatorLogin))
        {
            return true;
        }

        return record.OrganisationLogins.Any(login => allowedOrganisations.Contains(login));
    }

    /// <summary>Builds 05:00:00 UTC for a UTC date.</summary>
    public static DateTimeOffset OccasionInstant(DateOnly occasionDate)
        => new(occasionDate.Year, occasionDate.Month, occasionDate.Day, IDailyCollectionScheduler.Constants.OccasionHourUtc, 0, 0, TimeSpan.Zero);
}
