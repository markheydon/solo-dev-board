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
    public void IsEligibleAtFill_WhenQueuedOccasionSetForAnotherDay_ReturnsTrue()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-22",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };

        var eligible = CreateSut().IsEligibleAtFill(EligibleSignIn(), snapshot, Occasion);

        Assert.True(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenQueuedOccasionSetForToday_ReturnsFalse()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
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
    public void IsEligibleAtFill_WhenSignInAt0600_QueuedByFillAt0630()
    {
        var fillInstant = new DateTimeOffset(2026, 11, 23, 6, 30, 0, TimeSpan.Zero);
        var signIn = EligibleSignIn();
        signIn.CompletedAt = new DateTimeOffset(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);

        var eligible = CreateSut().IsEligibleAtFill(signIn, snapshot: null, fillInstant);

        Assert.True(eligible);
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

    [Fact]
    public void IsEligibleAtFill_WhenCredentialUnusable_ReturnsFalse()
    {
        var signIn = EligibleSignIn();
        signIn.CredentialUnusable = true;

        Assert.False(CreateSut().IsEligibleAtFill(signIn, snapshot: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInOlderThanThirtyDays_ReturnsFalse()
    {
        var signIn = EligibleSignIn();
        signIn.CompletedAt = Occasion.AddDays(-31);

        Assert.False(CreateSut().IsEligibleAtFill(signIn, snapshot: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInExactlyThirtyDaysBeforeOccasion_ReturnsTrue()
    {
        var signIn = EligibleSignIn();
        signIn.CompletedAt = Occasion.AddDays(-30);

        Assert.True(CreateSut().IsEligibleAtFill(signIn, snapshot: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenAdmissionEnabledAndBothListsEmpty_ReturnsFalse()
    {
        var sut = new HostedCollectionEligibilityEvaluator(
            Options.Create(new HostedAdmissionControlOptions { Enabled = true }));

        Assert.False(sut.IsEligibleAtFill(EligibleSignIn(), snapshot: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenAdmissionEnabledAndOrganisationOnSavedList_ReturnsTrue()
    {
        var signIn = EligibleSignIn();
        signIn.OrganisationLogins = ["my-org"];
        var sut = new HostedCollectionEligibilityEvaluator(
            Options.Create(new HostedAdmissionControlOptions
            {
                Enabled = true,
                AllowedOrganisationLogins = "my-org",
            }));

        Assert.True(sut.IsEligibleAtFill(signIn, snapshot: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenDailyInProgress_ReturnsFalse()
    {
        var snapshot = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.InProgress },
        };

        Assert.False(CreateSut().IsEligibleAtFill(EligibleSignIn(), snapshot, Occasion));
    }

    [Fact]
    public void IsEligibleAtRun_IgnoresSignInWindowAtDequeue()
    {
        var runInstant = new DateTimeOffset(2026, 11, 24, 6, 0, 0, TimeSpan.Zero);
        var signIn = EligibleSignIn();
        signIn.CompletedAt = runInstant.AddDays(-40);

        Assert.True(CreateSut().IsEligibleAtRun(signIn, runInstant));
    }
}
