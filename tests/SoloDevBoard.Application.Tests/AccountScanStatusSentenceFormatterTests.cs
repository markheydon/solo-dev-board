using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class AccountScanStatusSentenceFormatterTests
{
    private static readonly DateTimeOffset SampleUtc = new(2026, 6, 15, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LastSuccess = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly string[] ForbiddenSchedulePhrases =
    [
        "05:00 UTC",
        "daily occasion",
        "today",
        "tonight",
        "the following day",
    ];

    [Theory]
    [InlineData(ReportedAccountScanStatus.NeverRun, "No collection has run yet.")]
    [InlineData(ReportedAccountScanStatus.Scheduled, "A collection is waiting to start.")]
    public void Format_StatusSentences_MatchCatalogue(ReportedAccountScanStatus reported, string expectedStart)
    {
        var (status, schedule) = FormatHosted(reported);

        Assert.StartsWith(expectedStart, status, StringComparison.Ordinal);
        AssertForbidden(status);
        Assert.DoesNotContain("started it", status, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("You started", status, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_Running_DoesNotSayWhoStarted()
    {
        var runtime = Runtime(repositoriesRecorded: 4, runStartedAt: SampleUtc);
        var (status, _) = AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            OperatorSnapshotNoScheduleReason.None,
            ReportedAccountScanStatus.Running,
            ScanReport(ReportedAccountScanStatus.Running),
            runtime,
            EligibleSignIn(),
            nextAskIncludes: true,
            workerOffered: false);

        Assert.Contains("A collection is running.", status, StringComparison.Ordinal);
        Assert.Contains("4 repositories recorded so far.", status, StringComparison.Ordinal);
        AssertForbidden(status);
        Assert.DoesNotContain("You started", status, StringComparison.Ordinal);
        Assert.DoesNotContain("daily occasion", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_FinishedSuccess_OmitsStoredSuccessWord()
    {
        var runtime = Runtime(snapshotCount: 7, lastSuccess: LastSuccess);
        var (status, _) = AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            OperatorSnapshotNoScheduleReason.None,
            ReportedAccountScanStatus.Finished,
            ScanReport(
                ReportedAccountScanStatus.Finished,
                lastResult: AccountScanStatusDocumentConstants.LastResultSuccess,
                lastScanAt: SampleUtc),
            runtime,
            EligibleSignIn(),
            nextAskIncludes: true,
            workerOffered: false);

        Assert.Contains("finished successfully", status, StringComparison.Ordinal);
        Assert.Contains("7 repositories were saved.", status, StringComparison.Ordinal);
        Assert.DoesNotContain("Success", status, StringComparison.Ordinal);
        AssertForbidden(status);
    }

    [Fact]
    public void Format_FinishedFailure_UsesSignInOrTokenComment()
    {
        var (status, _) = FormatHosted(
            ReportedAccountScanStatus.Finished,
            lastResult: AccountScanStatusDocumentConstants.LastResultFailure,
            resultComment: "token");

        Assert.Contains("without saving a snapshot because of a sign-in or token problem.", status, StringComparison.Ordinal);
        AssertForbidden(status);
    }

    [Fact]
    public void Format_FinishedFailure_WithNewerWorkerToken_NotPausedSentence()
    {
        var (status, _) = FormatHosted(
            ReportedAccountScanStatus.Finished,
            lastResult: AccountScanStatusDocumentConstants.LastResultFailure,
            resultComment: "token");

        Assert.Contains("finished at", status, StringComparison.Ordinal);
        Assert.DoesNotContain("paused until you refresh", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_SuspendedToken_UsesPausedSentence()
    {
        var (status, _) = FormatHosted(
            ReportedAccountScanStatus.Suspended,
            lastResult: AccountScanStatusDocumentConstants.LastResultFailure,
            lastScanAt: SampleUtc);

        Assert.Contains("paused until you refresh the access used for the overnight collection.", status, StringComparison.Ordinal);
        Assert.Contains("sign-in or token problem", status, StringComparison.Ordinal);
        AssertForbidden(status);
    }

    [Fact]
    public void Format_SuspendedNotAllowed_UsesNotAllowedSentence()
    {
        var scan = ScanReport(ReportedAccountScanStatus.Suspended, suspensionReason: AccountScanSuspensionReason.NotAllowed);
        var (status, schedule) = AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            OperatorSnapshotNoScheduleReason.NotAllowed,
            ReportedAccountScanStatus.Suspended,
            scan,
            Runtime(),
            EligibleSignIn(),
            nextAskIncludes: false,
            workerOffered: false);

        Assert.Contains("paused because you are not allowed to use the product.", status, StringComparison.Ordinal);
        Assert.Equal(
            "No scheduled collection will run because you are not allowed to use the product.",
            schedule);
        AssertForbidden(status);
        AssertForbidden(schedule);
    }

    [Fact]
    public void Format_Stalled_UsesStalledSentence()
    {
        var (status, _) = FormatHosted(ReportedAccountScanStatus.Stalled);

        Assert.Contains("stopped reporting progress.", status, StringComparison.Ordinal);
        AssertForbidden(status);
    }

    [Theory]
    [InlineData(OperatorSnapshotNoScheduleReason.NotAllowed, "No scheduled collection will run because you are not allowed to use the product.")]
    [InlineData(OperatorSnapshotNoScheduleReason.CredentialUnusable, "No scheduled collection will run until you refresh the access used for the overnight collection.")]
    [InlineData(OperatorSnapshotNoScheduleReason.OutsideWindow, "No scheduled collection will run until you sign in again.")]
    public void Format_ScheduleRefusal_OrderMatchesDataModel(
        OperatorSnapshotNoScheduleReason reason,
        string expectedSchedule)
    {
        var (_, schedule) = AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            reason,
            ReportedAccountScanStatus.Finished,
            ScanReport(ReportedAccountScanStatus.Finished),
            Runtime(),
            EligibleSignIn(),
            nextAskIncludes: false,
            workerOffered: false);

        Assert.Equal(expectedSchedule, schedule);
        AssertForbidden(schedule);
    }

    [Fact]
    public void Format_NextAskIncludesAccount_ShowsOvernightSchedule()
    {
        var (_, schedule) = AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            OperatorSnapshotNoScheduleReason.None,
            ReportedAccountScanStatus.NeverRun,
            ScanReport(ReportedAccountScanStatus.NeverRun),
            Runtime(),
            EligibleSignIn(),
            nextAskIncludes: true,
            workerOffered: true);

        Assert.Equal("The next collection runs overnight.", schedule);
        AssertForbidden(schedule);
    }

    [Fact]
    public void Format_WithLastSuccessfulCollection_AppendsLastSuccessClause()
    {
        var (status, _) = FormatHosted(
            ReportedAccountScanStatus.Scheduled,
            lastSuccess: LastSuccess);

        Assert.Contains("The last successful snapshot was saved at", status, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WithoutLastSuccessfulCollection_OmitsLastSuccessClause()
    {
        var (status, _) = FormatHosted(ReportedAccountScanStatus.Scheduled);

        Assert.DoesNotContain("last successful snapshot", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_Scheduled_UsesOvernightScheduleSentence()
    {
        var (_, schedule) = FormatHosted(ReportedAccountScanStatus.Scheduled);

        Assert.Equal("The next collection runs overnight.", schedule);
    }

    private static (string? StatusSentence, string? ScheduleSentence) FormatHosted(
        ReportedAccountScanStatus reported,
        string? lastResult = null,
        string? resultComment = null,
        DateTimeOffset? lastScanAt = null,
        DateTimeOffset? lastSuccess = null)
    {
        var scan = ScanReport(reported, lastResult, resultComment, lastScanAt);
        return AccountScanStatusSentenceFormatter.Format(
            OperatorSnapshotSchedulePosture.Hosted,
            OperatorSnapshotNoScheduleReason.None,
            reported,
            scan,
            Runtime(lastSuccess: lastSuccess),
            EligibleSignIn(),
            nextAskIncludes: true,
            workerOffered: true);
    }

    private static OperatorSnapshotStatusDto Runtime(
        int repositoriesRecorded = 0,
        int snapshotCount = 0,
        DateTimeOffset? runStartedAt = null,
        DateTimeOffset? lastSuccess = null) => new()
        {
            RepositoriesRecordedThisRun = repositoriesRecorded,
            SnapshotRepositoryCount = snapshotCount,
            RunStartedAt = runStartedAt,
            LastSuccessfulCollectionAt = lastSuccess,
        };

    private static AccountScanStatusReport ScanReport(
        ReportedAccountScanStatus reported,
        string? lastResult = null,
        string? resultComment = null,
        DateTimeOffset? lastScanAt = null,
        AccountScanSuspensionReason suspensionReason = AccountScanSuspensionReason.None)
        => new(reported, null, lastScanAt, lastResult, resultComment, suspensionReason);

    private static HostedSignInRecordForSchedule EligibleSignIn() => new(
        "octocat",
        CompletedAt: DateTimeOffset.UtcNow.AddDays(-1),
        CredentialUnusable: false,
        HasWorkerRefreshToken: false,
        OrganisationLogins: []);

    private static void AssertForbidden(string? sentence)
    {
        Assert.NotNull(sentence);
        foreach (var phrase in ForbiddenSchedulePhrases)
        {
            Assert.DoesNotContain(phrase, sentence, StringComparison.OrdinalIgnoreCase);
        }
    }
}
