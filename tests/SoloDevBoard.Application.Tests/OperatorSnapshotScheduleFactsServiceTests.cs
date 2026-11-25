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

        var facts = CreateSut().Compute(document, signIn, null, utcNow);

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

        var facts = sut.Compute(document, signIn, null, utcNow);

        Assert.Equal(OperatorSnapshotNoScheduleReason.NotAllowed, facts.NoScheduleReason);
    }

    [Fact]
    public void Compute_RefusalOrder_CredentialUnusableBeforeOutsideWindow()
    {
        var utcNow = TodayOccasion.AddHours(2);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-45),
            CredentialUnusable: true,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument { OperatorLogin = "octocat" };

        var facts = CreateSut().Compute(document, signIn, null, utcNow);

        Assert.Equal(OperatorSnapshotNoScheduleReason.CredentialUnusable, facts.NoScheduleReason);
        Assert.Null(facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_WhenSnapshotNotYetCreated_UsesSignInLoginForAdmission()
    {
        var utcNow = TodayOccasion.AddHours(-2);
        var signIn = new HostedSignInRecordForSchedule(
            "markheydon",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var admission = new HostedCollectionEligibilityEvaluator(
            Options.Create(new HostedAdmissionControlOptions
            {
                Enabled = true,
                AllowedUserLogins = "markheydon",
            }));
        var sut = new OperatorSnapshotScheduleFactsService(
            new StubScheduledCollectionEnvironment { IsHostedScheduleEnabled = true },
            admission);

        var facts = sut.Compute(new OperatorSnapshotDocument(), signIn, null, utcNow);

        Assert.Equal(OperatorSnapshotNoScheduleReason.None, facts.NoScheduleReason);
        Assert.Equal(TodayOccasion, facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_WhenScheduledAfter0500_KeepsTodayOccasion()
    {
        var utcNow = TodayOccasion.AddHours(6);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument { OperatorLogin = "octocat" };

        var facts = CreateSut().Compute(document, signIn, ReportedAccountScanStatus.Scheduled, utcNow);

        Assert.Equal(TodayOccasion, facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_ScheduledWithSignInOlderThanThirtyDays_KeepsTodayAndDoesNotRefuse()
    {
        var utcNow = TodayOccasion.AddHours(1);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-45),
            CredentialUnusable: false,
            OrganisationLogins: []);

        var facts = CreateSut().Compute(new OperatorSnapshotDocument(), signIn, ReportedAccountScanStatus.Scheduled, utcNow);

        Assert.Equal(OperatorSnapshotNoScheduleReason.None, facts.NoScheduleReason);
        Assert.Equal(TodayOccasion, facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_ScheduledButNotAllowed_NotAllowedWins()
    {
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-1),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var sut = new OperatorSnapshotScheduleFactsService(
            new StubScheduledCollectionEnvironment { IsHostedScheduleEnabled = true },
            new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions
            {
                Enabled = true,
                AllowedUserLogins = "other-user",
            })));

        var facts = sut.Compute(new OperatorSnapshotDocument(), signIn, ReportedAccountScanStatus.Scheduled, TodayOccasion.AddHours(1));

        Assert.Equal(OperatorSnapshotNoScheduleReason.NotAllowed, facts.NoScheduleReason);
        Assert.Null(facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_AllowedNotScheduledOutsideWindowAtNamedInstant_ReportsOutsideWindow()
    {
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddDays(-45),
            CredentialUnusable: false,
            OrganisationLogins: []);

        var facts = CreateSut().Compute(
            new OperatorSnapshotDocument(),
            signIn,
            ReportedAccountScanStatus.Finished,
            TodayOccasion.AddHours(1));

        Assert.Equal(OperatorSnapshotNoScheduleReason.OutsideWindow, facts.NoScheduleReason);
        Assert.Null(facts.NextOccasionAtUtc);
    }

    [Fact]
    public void Compute_LeanPosture_ReportsLeanWithNoNextOccasion()
    {
        var sut = new OperatorSnapshotScheduleFactsService(
            new StubScheduledCollectionEnvironment { IsHostedScheduleEnabled = false },
            new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions())));

        var facts = sut.Compute(new OperatorSnapshotDocument(), null, null, TodayOccasion.AddHours(1));

        Assert.Equal(OperatorSnapshotSchedulePosture.Lean, facts.SchedulePosture);
        Assert.Equal(OperatorSnapshotNoScheduleReason.Lean, facts.NoScheduleReason);
        Assert.Null(facts.NextOccasionAtUtc);
    }

    private sealed class StubScheduledCollectionEnvironment : IScheduledCollectionEnvironment
    {
        public bool IsHostedScheduleEnabled { get; init; }
    }
}
