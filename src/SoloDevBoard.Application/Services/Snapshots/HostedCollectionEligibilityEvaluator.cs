using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Decides whether an operator is queued at daily fill.</summary>
public sealed class HostedCollectionEligibilityEvaluator(IOptions<HostedAdmissionControlOptions> admissionOptions)
{
    private readonly HostedAdmissionControlOptions _admissionOptions =
        admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));

    /// <summary>Returns true when the login may be queued for <paramref name="occasionInstant"/>.</summary>
    public bool IsEligibleAtFill(
        HostedSignInRecord signInRecord,
        OperatorSnapshotDocument? snapshot,
        DateTimeOffset occasionInstant)
    {
        ArgumentNullException.ThrowIfNull(signInRecord);

        if (signInRecord.CredentialUnusable)
        {
            return false;
        }

        if (!IsInsideSignInWindow(signInRecord.CompletedAt, occasionInstant))
        {
            return false;
        }

        if (!IsAllowedByAdmission(signInRecord))
        {
            return false;
        }

        if (snapshot is null)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(snapshot.QueuedOccasion))
        {
            return false;
        }

        if (snapshot.Run.Status == OperatorSnapshotRunStatus.InProgress)
        {
            return false;
        }

        return true;
    }

    /// <summary>Admission verdict for schedule facts (no snapshot flags).</summary>
    public bool WouldBeEligibleForSchedule(HostedSignInRecord signInRecord, DateTimeOffset occasionInstant)
    {
        ArgumentNullException.ThrowIfNull(signInRecord);
        if (signInRecord.CredentialUnusable)
        {
            return false;
        }

        if (!IsInsideSignInWindow(signInRecord.CompletedAt, occasionInstant))
        {
            return false;
        }

        return IsAllowedByAdmission(signInRecord);
    }

    private static bool IsInsideSignInWindow(DateTimeOffset completedAt, DateTimeOffset occasionInstant)
    {
        var windowStart = occasionInstant.AddDays(-30);
        return completedAt >= windowStart;
    }

    private bool IsAllowedByAdmission(HostedSignInRecord signInRecord)
    {
        if (!_admissionOptions.Enabled)
        {
            return true;
        }

        var allowedUsers = HostedAdmissionAllowList.ParseNormalisedSet(_admissionOptions.AllowedUserLogins);
        var allowedOrganisations = HostedAdmissionAllowList.ParseNormalisedSet(_admissionOptions.AllowedOrganisationLogins);
        if (allowedUsers.Count == 0 && allowedOrganisations.Count == 0)
        {
            return false;
        }

        if (allowedUsers.Contains(signInRecord.OperatorLogin))
        {
            return true;
        }

        foreach (var organisationLogin in signInRecord.OrganisationLogins)
        {
            if (allowedOrganisations.Contains(organisationLogin))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Parses admission lists without taking an Infrastructure dependency from Application tests.</summary>
public static class HostedAdmissionAllowList
{
    private static readonly char[] EntrySeparators = [',', ';', ' '];

    /// <summary>Normalised allow-list entries.</summary>
    public static HashSet<string> ParseNormalisedSet(string? value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var entry in value.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(entry) && entry != "-")
            {
                result.Add(entry);
            }
        }

        return result;
    }
}
