using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class AccountScanStatusReporterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AccountScanStatusReporter_MarkAgeExactlyFiveMinutes_ReportsRunning()
    {
        var document = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 1,
            livenessAt: Now.AddMinutes(-5),
            runningSince: Now.AddMinutes(-20));
        var sut = new AccountScanStatusReporter(new FixedTimeProvider(Now));

        Assert.Equal(ReportedAccountScanStatus.Running, sut.Report(document));
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, document.Status);
    }

    [Fact]
    public void AccountScanStatusReporter_MarkAgeMoreThanFiveMinutes_ReportsStalled()
    {
        var document = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 1,
            livenessAt: Now.AddMinutes(-5).AddSeconds(-1),
            runningSince: Now.AddMinutes(-20));
        var sut = new AccountScanStatusReporter(new FixedTimeProvider(Now));

        Assert.Equal(ReportedAccountScanStatus.Stalled, sut.Report(document));
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, document.Status);
    }

    [Fact]
    public void AccountScanStatusReporter_MissingMarkUsesRunningSinceOlderThanFiveMinutes_ReportsStalled()
    {
        var document = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 1,
            livenessAt: null,
            runningSince: Now.AddMinutes(-6));
        var sut = new AccountScanStatusReporter(new FixedTimeProvider(Now));

        Assert.Equal(ReportedAccountScanStatus.Stalled, sut.Report(document));
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, document.Status);
    }

    [Fact]
    public async Task AccountScanStatusReporter_SignInRecordWithoutStatusBlob_ReportsNeverRunAndDoesNotWrite()
    {
        var store = new InMemoryAccountScanStatusStore();
        var time = new FixedTimeProvider(Now);
        var transitions = AccountScanStatusTestFactory.CreateTransitions(store, time);

        var report = await transitions.GetReportAsync(
            "octocat",
            hostedSignInRecordExists: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReportedAccountScanStatus.NeverRun, report.Reported);
        Assert.Equal(0, store.WriteCount);
        Assert.Null(store.Peek("octocat"));
    }
}
