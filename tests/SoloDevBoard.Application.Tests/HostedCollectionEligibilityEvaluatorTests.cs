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
        SchemaVersion = 2,
        WorkerRefreshToken = "token",
        CredentialUnusable = false,
    };

    [Fact]
    public void IsEligibleAtFill_WhenReportedFinished_ReturnsTrue()
    {
        var eligible = CreateSut().IsEligibleAtFill(
            EligibleSignIn(),
            ReportedAccountScanStatus.Finished,
            Occasion);

        Assert.True(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenReportedScheduled_ReturnsFalse()
    {
        var eligible = CreateSut().IsEligibleAtFill(
            EligibleSignIn(),
            ReportedAccountScanStatus.Scheduled,
            Occasion);

        Assert.False(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenReportedRunning_ReturnsFalse()
    {
        var eligible = CreateSut().IsEligibleAtFill(
            EligibleSignIn(),
            ReportedAccountScanStatus.Running,
            Occasion);

        Assert.False(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenReportedStalled_ReturnsTrue()
    {
        var eligible = CreateSut().IsEligibleAtFill(
            EligibleSignIn(),
            ReportedAccountScanStatus.Stalled,
            Occasion);

        Assert.True(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInAt0600_QueuedByFillAt0630()
    {
        var fillInstant = new DateTimeOffset(2026, 11, 23, 6, 30, 0, TimeSpan.Zero);
        var signIn = EligibleSignIn();
        signIn.CompletedAt = new DateTimeOffset(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);

        var eligible = CreateSut().IsEligibleAtFill(signIn, reportedScanStatus: null, fillInstant);

        Assert.True(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenEligibleAndNoBlockingReport_ReturnsTrue()
    {
        var eligible = CreateSut().IsEligibleAtFill(
            EligibleSignIn(),
            ReportedAccountScanStatus.NeverRun,
            Occasion);

        Assert.True(eligible);
    }

    [Fact]
    public void IsEligibleAtFill_WhenCredentialUnusable_ReturnsFalse()
    {
        var signIn = EligibleSignIn();
        signIn.CredentialUnusable = true;

        Assert.False(CreateSut().IsEligibleAtFill(signIn, reportedScanStatus: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInOlderThanThirtyDays_ReturnsFalse()
    {
        var signIn = EligibleSignIn();
        signIn.CompletedAt = Occasion.AddDays(-31);

        Assert.False(CreateSut().IsEligibleAtFill(signIn, reportedScanStatus: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenSignInExactlyThirtyDaysBeforeOccasion_ReturnsTrue()
    {
        var signIn = EligibleSignIn();
        signIn.CompletedAt = Occasion.AddDays(-30);

        Assert.True(CreateSut().IsEligibleAtFill(signIn, reportedScanStatus: null, Occasion));
    }

    [Fact]
    public void IsEligibleAtFill_WhenAdmissionEnabledAndBothListsEmpty_ReturnsFalse()
    {
        var sut = new HostedCollectionEligibilityEvaluator(
            Options.Create(new HostedAdmissionControlOptions { Enabled = true }));

        Assert.False(sut.IsEligibleAtFill(EligibleSignIn(), reportedScanStatus: null, Occasion));
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

        Assert.True(sut.IsEligibleAtFill(signIn, reportedScanStatus: null, Occasion));
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
