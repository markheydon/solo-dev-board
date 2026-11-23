using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class HostedCollectionEligibilityEvaluatorTests
{
    private static readonly DateTimeOffset Occasion = new(2026, 11, 23, 5, 0, 0, TimeSpan.Zero);

    private static HostedCollectionEligibilityEvaluator CreateSut() =>
        new(Options.Create(new HostedAdmissionControlOptions { Enabled = false }));

    private static HostedSignInRecord EligibleSignIn() => new()
    {
        OperatorLogin = "octocat",
        CompletedAt = Occasion.AddDays(-1),
        OrganisationLogins = [],
        RefreshToken = "token",
        CredentialUnusable = false,
    };

    [Fact]
    public void IsEligibleAtFill_WhenQueuedOccasionSetForAnotherDay_ReturnsFalse()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-22",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };

        var eligible = CreateSut().IsEligibleAtFill(EligibleSignIn(), snapshot, Occasion);

        Assert.False(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenHandStartedInProgress_ReturnsFalse()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Operator,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.InProgress },
        };

        var eligible = CreateSut().IsEligibleAtFill(EligibleSignIn(), snapshot, Occasion);

        Assert.False(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInCompletedAfterTodaysOccasionStart_ReturnsFalse()
    {
        var fillInstant = new DateTimeOffset(2026, 11, 23, 5, 10, 0, TimeSpan.Zero);
        var signIn = EligibleSignIn();
        signIn.CompletedAt = fillInstant;

        var eligible = CreateSut().IsEligibleAtFill(signIn, snapshot: null, fillInstant);

        Assert.False(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenEligibleAndNoBlockingFlags_ReturnsTrue()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };

        var eligible = CreateSut().IsEligibleAtFill(EligibleSignIn(), snapshot, Occasion);

        Assert.True(eligible);
    }
}