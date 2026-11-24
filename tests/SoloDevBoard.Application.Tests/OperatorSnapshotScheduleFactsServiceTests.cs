using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotScheduleFactsServiceTests
{
    private static readonly DateTimeOffset TodayOccasion = new(2026, 11, 23, 5, 0, 0, TimeSpan.Zero);

    private static OperatorSnapshotScheduleFactsService CreateSut() =>
        new(
            new StubScheduledCollectionEnvironment { IsHostedScheduleEnabled = true },
            new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions { Enabled = false })));

    [Fact]
    public void Compute_AfterAskPassed_NamesFollowingDayWhetherOrNotScanSucceeded()
    {
        var utcNow = TodayOccasion.AddMinutes(12);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        };

        var facts = CreateSut().Compute(document, signIn, utcNow);

        Assert.Equal(TodayOccasion.AddDays(1), facts.NextOccasionAtUtc);
        Assert.Equal(OperatorSnapshotNoScheduleReason.None, facts.NoScheduleReason);
    }

    [Fact]
    public void Compute_RefusalOrder_NotAllowedBeforeCredentialUnusable()
    {
        var utcNow = TodayOccasion.AddHours(2);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: true,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument { OperatorLogin = "octocat" };
        var admission = new HostedCollectionEligibilityEvaluator(
            Microsoft.Extensions.Options.Options.Create(new HostedAdmissionControlOptions
            {
                Enabled = true,
                AllowedUserLogins = "other-user",
            }));
        var sut = new OperatorSnapshotScheduleFactsService(
            new StubScheduledCollectionEnvironment { IsHostedScheduleEnabled = true },
            admission);

        var facts = sut.Compute(document, signIn, utcNow);

        Assert.Equal(OperatorSnapshotNoScheduleReason.NotAllowed, facts.NoScheduleReason);
    }

    [Fact]
    public void Compute_WhenQueuedAfter0500_KeepsTodayOccasion()
    {
        var utcNow = TodayOccasion.AddHours(6);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
        };

        var facts = CreateSut().Compute(document, signIn, utcNow);

        Assert.Equal(TodayOccasion, facts.NextOccasionAtUtc);
    }

    private sealed class StubScheduledCollectionEnvironment : IScheduledCollectionEnvironment
    {
        public bool IsHostedScheduleEnabled { get; init; }
    }
}
