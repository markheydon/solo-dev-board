using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotEligibilityTests
{
    [Fact]
    public void IsEligibleAtFill_WhenCompletedExactly30DaysAgo_IsEligible()
    {
        var occasion = new DateTimeOffset(2026, 6, 1, 5, 0, 0, TimeSpan.Zero);
        var record = new OperatorSignInRecord
        {
            OperatorLogin = "alice",
            CompletedAt = occasion.AddDays(-30),
        };

        var eligibility = CreateEligibility(admissionEnabled: false);
        Assert.True(eligibility.IsEligibleAtFill(record, occasion, queue: null, snapshot: null));
    }

    [Fact]
    public void IsEligibleAtFill_WhenCompletedAfterOccasion_IsExcluded()
    {
        var occasion = new DateTimeOffset(2026, 6, 1, 5, 0, 0, TimeSpan.Zero);
        var record = new OperatorSignInRecord
        {
            OperatorLogin = "alice",
            CompletedAt = occasion.AddMinutes(10),
        };

        var eligibility = CreateEligibility(admissionEnabled: false);
        Assert.False(eligibility.IsEligibleAtFill(record, occasion, queue: null, snapshot: null));
    }

    [Fact]
    public void IsAdmissionAllowed_WhenAdmissionOnAndListsEmpty_AllowsNobody()
    {
        var eligibility = CreateEligibility(admissionEnabled: true, users: string.Empty, orgs: string.Empty);
        var record = new OperatorSignInRecord { OperatorLogin = "alice", OrganisationLogins = ["my-org"] };
        Assert.False(eligibility.IsAdmissionAllowed(record));
    }

    private static OperatorSnapshotEligibility CreateEligibility(
        bool admissionEnabled,
        string users = "alice",
        string orgs = "")
        => new(Options.Create(new HostedAdmissionControlOptions
        {
            Enabled = admissionEnabled,
            AllowedUserLogins = users,
            AllowedOrganisationLogins = orgs,
        }));
}
