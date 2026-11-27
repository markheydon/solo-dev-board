using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class AccountScanStatusTransitionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AccountScanStatusTransitionService_SignInWhenBlobAbsent_InsertsNeverRun()
    {
        var store = new InMemoryAccountScanStatusStore();
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, new FixedTimeProvider(Now));

        await sut.EnsureNeverRunIfAbsentAsync("octocat", TestContext.Current.CancellationToken);

        var stored = store.Peek("octocat");
        Assert.NotNull(stored);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusNeverRun, stored.Status);
        Assert.Null(stored.LastScanAt);
        Assert.Null(stored.LastResult);
        Assert.Equal(string.Empty, stored.ResultComment);
        Assert.Null(stored.SuspensionReason);
        Assert.Equal(0, stored.ScanGeneration);
        Assert.False(stored.QueueMessagePending);
        Assert.Null(stored.QueueVisibleAfter);
        Assert.Null(stored.LivenessAt);
        Assert.Null(stored.RunningSince);
    }

    [Fact]
    public async Task AccountScanStatusTransitionService_SignInWhenBlobPresent_LeavesBlobUnchanged()
    {
        var store = new InMemoryAccountScanStatusStore();
        var existing = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusFinished,
            generation: 7,
            lastResult: AccountScanStatusDocumentConstants.LastResultSuccess);
        existing.LastScanAt = Now.AddDays(-1);
        existing.ResultComment = AccountScanStatusDocumentConstants.CommentSuccess;
        store.Seed(existing);
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, new FixedTimeProvider(Now));

        await sut.EnsureNeverRunIfAbsentAsync("octocat", TestContext.Current.CancellationToken);

        Assert.Equal(0, store.WriteCount);
        var stored = store.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, stored.Status);
        Assert.Equal(7, stored.ScanGeneration);
        Assert.Equal(Now.AddDays(-1), stored.LastScanAt);
    }

    [Fact]
    public async Task AccountScanStatusTransitionService_StillWorkingPastSixtySeconds_WritesAnotherLivenessMark()
    {
        var time = new FixedTimeProvider(Now);
        var store = new InMemoryAccountScanStatusStore();
        store.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 4,
            livenessAt: Now,
            runningSince: Now));
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, time);

        time.SetUtcNow(Now.AddSeconds(10));
        await sut.TouchLivenessIfDueAsync("octocat", 4, TestContext.Current.CancellationToken);
        Assert.Equal(Now, store.Peek("octocat")!.LivenessAt);

        time.SetUtcNow(Now.AddSeconds(30));
        await sut.TouchLivenessIfDueAsync("octocat", 4, TestContext.Current.CancellationToken);

        var stored = store.Peek("octocat")!;
        Assert.Equal(Now.AddSeconds(30), stored.LivenessAt);
        Assert.Equal(Now, stored.RunningSince);
        Assert.Equal(4, stored.ScanGeneration);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
    }

    [Fact]
    public async Task AccountScanStatusTransitionService_LivenessForOlderGeneration_IsIgnored()
    {
        var time = new FixedTimeProvider(Now.AddMinutes(1));
        var store = new InMemoryAccountScanStatusStore();
        store.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 5,
            livenessAt: Now,
            runningSince: Now));
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, time);

        await sut.TouchLivenessIfDueAsync("octocat", 4, TestContext.Current.CancellationToken);

        Assert.Equal(Now, store.Peek("octocat")!.LivenessAt);
    }

    [Fact]
    public async Task AccountScanStatusTransitionService_HandStartWhileScheduledOrRunning_IsRefused()
    {
        var store = new InMemoryAccountScanStatusStore();
        store.Seed(AccountScanStatusTestFactory.Document(
            "scheduled", AccountScanStatusDocumentConstants.StatusScheduled, 1, queueMessagePending: true));
        store.Seed(AccountScanStatusTestFactory.Document(
            "running", AccountScanStatusDocumentConstants.StatusRunning, 1, livenessAt: Now, runningSince: Now));
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, new FixedTimeProvider(Now));

        var scheduled = await sut.TryWinRunningAsync(
            "scheduled", OperatorSnapshotCollectionTrigger.Operator, fromHandStart: true, TestContext.Current.CancellationToken);
        var running = await sut.TryWinRunningAsync(
            "running", OperatorSnapshotCollectionTrigger.Operator, fromHandStart: true, TestContext.Current.CancellationToken);

        Assert.IsType<AccountScanRunningWinResult.Refused>(scheduled);
        Assert.IsType<AccountScanRunningWinResult.Refused>(running);
        Assert.Equal(1, store.Peek("scheduled")!.ScanGeneration);
    }

    [Theory]
    [InlineData(AccountScanStatusDocumentConstants.StatusNeverRun)]
    [InlineData(AccountScanStatusDocumentConstants.StatusFinished)]
    [InlineData(AccountScanStatusDocumentConstants.StatusSuspended)]
    public async Task AccountScanStatusTransitionService_HandStartFromIdleStatus_WinsRunningWithNewGeneration(string status)
    {
        var store = new InMemoryAccountScanStatusStore();
        store.Seed(AccountScanStatusTestFactory.Document("octocat", status, generation: 2));
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, new FixedTimeProvider(Now));

        var result = await sut.TryWinRunningAsync(
            "octocat", OperatorSnapshotCollectionTrigger.Operator, fromHandStart: true, TestContext.Current.CancellationToken);

        var won = Assert.IsType<AccountScanRunningWinResult.Won>(result);
        Assert.Equal(3, won.Generation);
        var stored = store.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
        Assert.False(stored.QueueMessagePending);
        Assert.Equal(Now, stored.LivenessAt);
        Assert.Equal(Now, stored.RunningSince);
    }

    [Fact]
    public async Task AccountScanStatusTransitionService_HandStartLostEtag_DoesNotRetry()
    {
        var store = new InMemoryAccountScanStatusStore();
        store.Seed(AccountScanStatusTestFactory.Document("octocat", AccountScanStatusDocumentConstants.StatusFinished, 2));
        store.ConflictOnNextReplace = true;
        var sut = AccountScanStatusTestFactory.CreateTransitions(store, new FixedTimeProvider(Now));

        var result = await sut.TryWinRunningAsync(
            "octocat", OperatorSnapshotCollectionTrigger.Operator, fromHandStart: true, TestContext.Current.CancellationToken);

        Assert.IsType<AccountScanRunningWinResult.LostRace>(result);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, store.Peek("octocat")!.Status);
    }
}
