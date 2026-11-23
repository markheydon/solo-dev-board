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
    public void Compute_WhenSignInCompletedAfterTodaysOccasionStart_NamesNextDayOccasion()
    {
        var utcNow = TodayOccasion.AddMinutes(12);
        var signIn = new HostedSignInRecordForSchedule(
            "octocat",
            CompletedAt: TodayOccasion.AddMinutes(10),
            CredentialUnusable: false,
            OrganisationLogins: []);
        var document = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };

        var facts = CreateSut().Compute(document, signIn, utcNow);

        Assert.Equal(TodayOccasion.AddDays(1), facts.NextOccasionAtUtc);
    }

    private sealed class StubScheduledCollectionEnvironment : IScheduledCollectionEnvironment
    {
        public bool IsHostedScheduleEnabled { get; init; }
    }
}