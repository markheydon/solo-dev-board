using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class DailySnapshotCollectionPassServiceTests
{
    private const string Login = "octocat";

    [Fact]
    public async Task HandleQueueMessage_WhenUnmetFillVisible_CallsFill()
    {
        var dailyOccasion = Substitute.For<IDailyOccasionService>();
        dailyOccasion.TryFillDailyOccasionAsync(Arg.Any<CancellationToken>())
            .Returns(DailyOccasionFillResult.Filled);

        var completed = false;
        var harness = new Harness { DailyOccasion = dailyOccasion };
        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(new DailySnapshotQueueMessage { Kind = "unmetFill" }, () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        await dailyOccasion.Received(1).TryFillDailyOccasionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_DequeueAllowed_SetsRunningAndLivenessMark()
    {
        var gate = new TaskCompletionSource();
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 3));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));
        var completed = false;

        var handling = harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => harness.Statuses.Peek(Login)!.Status == AccountScanStatusDocumentConstants.StatusRunning);
        var running = harness.Statuses.Peek(Login)!;
        Assert.NotNull(running.LivenessAt);
        Assert.NotNull(running.RunningSince);
        Assert.Equal(3, running.ScanGeneration);
        Assert.Null(running.SuspensionReason);
        Assert.Equal(OperatorSnapshotCollectionTrigger.Daily, harness.Snapshots.GetDocument(Login)!.Trigger);

        gate.SetResult();
        await handling;

        Assert.True(completed);
        var finished = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, finished.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultSuccess, finished.LastResult);
        Assert.Equal(AccountScanStatusDocumentConstants.CommentSuccess, finished.ResultComment);
        Assert.NotNull(finished.LastScanAt);
        Assert.Null(finished.LivenessAt);
        Assert.Null(finished.RunningSince);
        Assert.False(finished.QueueMessagePending);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_NotAllowed_SetsSuspendedWithoutRunningAndClearsQueueMessagePending()
    {
        var lastScan = DateTimeOffset.UtcNow.AddDays(-1);
        var scheduled = Scheduled(generation: 2);
        scheduled.LastScanAt = lastScan;
        scheduled.LastResult = AccountScanStatusDocumentConstants.LastResultSuccess;
        scheduled.ResultComment = AccountScanStatusDocumentConstants.CommentSuccess;
        var harness = new Harness
        {
            Admission = new HostedAdmissionControlOptions { Enabled = true, AllowedUserLogins = "other-user" },
        };
        harness.Statuses.Seed(scheduled);
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, stored.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.SuspensionNotAllowed, stored.SuspensionReason);
        Assert.False(stored.QueueMessagePending);
        Assert.Null(stored.LivenessAt);
        Assert.Equal(lastScan, stored.LastScanAt);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultSuccess, stored.LastResult);
        Assert.Equal(AccountScanStatusDocumentConstants.CommentSuccess, stored.ResultComment);
        var snapshot = harness.Snapshots.GetDocument(Login)!;
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, snapshot.Run.Status);
        await harness.TokenExchange.DidNotReceive().RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_StoredAccessUnusable_SetsSuspendedFailureAndCredentialUnusable()
    {
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.TokenExchange.RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<HostedGitHubRefreshTokenExchangeResult>>(_ => throw new InvalidOperationException("expired"));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, stored.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultFailure, stored.LastResult);
        Assert.False(string.IsNullOrWhiteSpace(stored.ResultComment));
        Assert.Null(stored.SuspensionReason);
        Assert.NotNull(stored.LastScanAt);
        Assert.False(stored.QueueMessagePending);
        Assert.Null(stored.LivenessAt);
        Assert.True(harness.SignIns.Single().CredentialUnusable);
        Assert.Empty(harness.Queue.Messages);
        Assert.Equal(OperatorSnapshotRunReason.SignInOrToken, harness.Snapshots.GetDocument(Login)!.Run.Reason);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenSignInRecordMissing_SuspendsWithSignInOrTokenFailure()
    {
        var harness = new Harness { SeedSignIn = false };
        harness.Statuses.Seed(Scheduled(generation: 1));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, stored.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultFailure, stored.LastResult);
        Assert.Equal(OperatorSnapshotRunReason.SignInOrToken, harness.Snapshots.GetDocument(Login)!.Run.Reason);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenSignInOutsideWindowAtDequeue_AttemptsPass()
    {
        var harness = new Harness { SignInCompletedAt = DateTimeOffset.UtcNow.AddDays(-40) };
        harness.Statuses.Seed(Scheduled(generation: 1));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        var gate = new TaskCompletionSource();
        gate.SetResult();
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => { }),
            TestContext.Current.CancellationToken);

        await harness.TokenExchange.Received(1).RefreshAccessTokenAsync("refresh", Arg.Any<CancellationToken>());
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, harness.Statuses.Peek(Login)!.Status);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenStatusNotScheduled_CompletesWithoutStartingPass()
    {
        var harness = new Harness();
        harness.Statuses.Seed(AccountScanStatusTestFactory.Document(
            Login,
            AccountScanStatusDocumentConstants.StatusFinished,
            generation: 2,
            lastResult: AccountScanStatusDocumentConstants.LastResultSuccess));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        Assert.Empty(harness.Queue.Messages);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, harness.Statuses.Peek(Login)!.Status);
        await harness.TokenExchange.DidNotReceive().RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleQueueMessage_WhenRetryNotBeforeStillAhead_RequeuesWithoutStartingPass()
    {
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 1, queueVisibleAfter: DateTimeOffset.UtcNow.AddMinutes(5)));
        var completed = false;
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(5);

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(
                new DailySnapshotQueueMessage
                {
                    OperatorLogin = Login,
                    OccasionDate = "2026-11-23",
                    Kind = "retry",
                    NotBefore = DailySnapshotRetryNotBefore.ToIso8601Utc(notBefore),
                },
                () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var requeued = Assert.Single(harness.Queue.Messages);
        Assert.Equal("retry", requeued.Kind);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, harness.Statuses.Peek(Login)!.Status);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenRunnerLeaseBusy_RequeuesSamePayloadAndStaysScheduled()
    {
        var harness = new Harness { Runner = new NeverAvailableRunnerLease() };
        harness.Statuses.Seed(Scheduled(generation: 1));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var requeued = Assert.Single(harness.Queue.Messages);
        Assert.Equal(Login, requeued.OperatorLogin);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, harness.Statuses.Peek(Login)!.Status);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenNotAllowedForRetryWaiting_RestoresRetainedRun()
    {
        var priorSuccess = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var harness = new Harness
        {
            Admission = new HostedAdmissionControlOptions { Enabled = true, AllowedUserLogins = "other-user" },
        };
        harness.Statuses.Seed(Scheduled(generation: 2, queueVisibleAfter: DateTimeOffset.UtcNow.AddMinutes(-1)));
        harness.Snapshots.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            AttemptsUsed = 2,
            RetainedRun = new OperatorSnapshotRetainedRun
            {
                Trigger = OperatorSnapshotCollectionTrigger.Operator,
                Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded, FinishedAt = priorSuccess },
            },
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.InProgress, Reason = OperatorSnapshotRunReason.Unexpected },
        });

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(new DailySnapshotQueueMessage { OperatorLogin = Login, OccasionDate = "2026-11-23", Kind = "retry" }, () => { }),
            TestContext.Current.CancellationToken);

        var document = harness.Snapshots.GetDocument(Login)!;
        Assert.Equal(OperatorSnapshotCollectionTrigger.Operator, document.Trigger);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
        Assert.Null(document.AttemptsUsed);
        Assert.Null(document.RetainedRun);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, harness.Statuses.Peek(Login)!.Status);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_RetryWaiting_SetsScheduledNotRunning()
    {
        var gate = new TaskCompletionSource();
        gate.SetResult();
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 5));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(
            CoordinatorTestFactory.Gated(gate, new InvalidOperationException("boom")));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, stored.Status);
        Assert.True(stored.QueueMessagePending);
        Assert.NotNull(stored.QueueVisibleAfter);
        Assert.Null(stored.LastResult);
        Assert.Null(stored.LivenessAt);
        Assert.Equal(5, stored.ScanGeneration);
        var retry = Assert.Single(harness.Queue.Messages);
        Assert.Equal("retry", retry.Kind);
        Assert.False(string.IsNullOrEmpty(retry.NotBefore));
        var snapshot = harness.Snapshots.GetDocument(Login)!;
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, snapshot.Run.Status);
        Assert.Equal(1, snapshot.AttemptsUsed);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_AttemptsExhausted_SetsFinishedThenUnmetFill()
    {
        var gate = new TaskCompletionSource();
        gate.SetResult();
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 5, queueVisibleAfter: DateTimeOffset.UtcNow.AddMinutes(-1)));
        var snapshot = SucceededSnapshot();
        snapshot.AttemptsUsed = 3;
        snapshot.Trigger = OperatorSnapshotCollectionTrigger.Daily;
        snapshot.RetainedRun = new OperatorSnapshotRetainedRun
        {
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };
        harness.Snapshots.Seed(Login, snapshot);
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(
            CoordinatorTestFactory.Gated(gate, new InvalidOperationException("boom")));

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(new DailySnapshotQueueMessage { OperatorLogin = Login, OccasionDate = "2026-11-23", Kind = "retry" }, () => { }),
            TestContext.Current.CancellationToken);

        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, stored.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultFailure, stored.LastResult);
        Assert.False(string.IsNullOrWhiteSpace(stored.ResultComment));
        Assert.NotNull(stored.LastScanAt);
        Assert.False(stored.QueueMessagePending);
        var unmet = Assert.Single(harness.Queue.Messages);
        Assert.Equal("unmetFill", unmet.Kind);
        Assert.True(DailySnapshotRetryNotBefore.TryParseIso8601Utc(unmet.NotBefore, out var notBefore));
        Assert.True(notBefore > DateTimeOffset.UtcNow.AddMinutes(14));
        Assert.Equal(OperatorSnapshotRunStatus.Failed, harness.Snapshots.GetDocument(Login)!.Run.Status);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_WorkerRedeliveryWhileRunning_CompletesMessageAndLeavesRunning()
    {
        var harness = new Harness();
        var running = AccountScanStatusTestFactory.Document(
            Login,
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 3,
            queueMessagePending: true,
            livenessAt: DateTimeOffset.UtcNow.AddSeconds(-20),
            runningSince: DateTimeOffset.UtcNow.AddMinutes(-1));
        harness.Statuses.Seed(running);
        var snapshot = SucceededSnapshot();
        snapshot.RetainedRun = new OperatorSnapshotRetainedRun
        {
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        };
        harness.Snapshots.Seed(Login, snapshot);
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
        Assert.False(stored.QueueMessagePending);
        Assert.Equal(3, stored.ScanGeneration);
        Assert.Null(stored.LastResult);
        Assert.Empty(harness.Queue.Messages);
        var document = harness.Snapshots.GetDocument(Login)!;
        Assert.NotNull(document.RetainedRun);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_LivenessLapse_DoesNotEnqueueUnmetFill()
    {
        var harness = new Harness();
        harness.Statuses.Seed(AccountScanStatusTestFactory.Document(
            Login,
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 3,
            queueMessagePending: true,
            livenessAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            runningSince: DateTimeOffset.UtcNow.AddMinutes(-12)));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        Assert.Empty(harness.Queue.Messages);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, harness.Statuses.Peek(Login)!.Status);
        var reporter = new AccountScanStatusReporter(TimeProvider.System);
        Assert.Equal(ReportedAccountScanStatus.Stalled, reporter.Report(harness.Statuses.Peek(Login)!));
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_EarlierGenerationAfterNewerScan_DoesNotOverwriteStatusMarkOrSnapshot()
    {
        var gate = new TaskCompletionSource();
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 1));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));

        var handling = harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => { }),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => harness.Statuses.Peek(Login)!.Status == AccountScanStatusDocumentConstants.StatusRunning);

        var newerMark = DateTimeOffset.UtcNow.AddSeconds(5);
        harness.Statuses.Seed(AccountScanStatusTestFactory.Document(
            Login,
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 2,
            livenessAt: newerMark,
            runningSince: newerMark));
        var lastSuccess = harness.Snapshots.GetDocument(Login)!.LastSuccessfulCollectionAt;
        gate.SetResult();
        await handling;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(2, stored.ScanGeneration);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
        Assert.Equal(newerMark, stored.LivenessAt);
        Assert.Null(stored.LastResult);
        var snapshot = harness.Snapshots.GetDocument(Login)!;
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, snapshot.Run.Status);
        Assert.Equal(lastSuccess, snapshot.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task HostedSignInRecord_Schema1RefreshToken_IsNotCopiedOrExchanged()
    {
        const string legacyRefresh = "schema1-browser-refresh-token";
        var harness = new Harness { SeedSignIn = false };
        harness.SignInStore.Seed(new HostedSignInRecord
        {
            SchemaVersion = 1,
            OperatorLogin = Login,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LegacyRefreshToken = legacyRefresh,
            CredentialUnusable = false,
        });
        var signIn = harness.SignIns.Single();
        Assert.Null(signIn.WorkerRefreshToken);
        Assert.Null(signIn.WorkerRefreshTokenForExchange);
        Assert.Equal(legacyRefresh, signIn.LegacyRefreshToken);

        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        await harness.TokenExchange.DidNotReceive()
            .RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await harness.TokenExchange.DidNotReceive()
            .RefreshAccessTokenAsync(legacyRefresh, Arg.Any<CancellationToken>());
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, harness.Statuses.Peek(Login)!.Status);
        Assert.True(harness.SignIns.Single().CredentialUnusable);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_MissingWorkerToken_SetsSuspendedWithoutCookieExchange()
    {
        var harness = new Harness { SeedSignIn = false };
        harness.SignInStore.Seed(new HostedSignInRecord
        {
            SchemaVersion = 2,
            OperatorLogin = Login,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CredentialUnusable = false,
        });
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        await harness.TokenExchange.DidNotReceive()
            .RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, harness.Statuses.Peek(Login)!.Status);
        Assert.True(harness.SignIns.Single().CredentialUnusable);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_QueuedScan_UsesWorkerTokenStoredWhenTaken()
    {
        var harness = new Harness { SeedSignIn = false };
        harness.SignInStore.Seed(new HostedSignInRecord
        {
            SchemaVersion = 2,
            OperatorLogin = Login,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CredentialUnusable = false,
        });
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());

        var storedRecord = harness.SignIns.Single();
        storedRecord.WorkerRefreshToken = "worker-at-dequeue";
        await harness.SignInStore.UpsertAsync(storedRecord, "etag", TestContext.Current.CancellationToken);

        var gate = new TaskCompletionSource();
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));
        var completed = false;

        var handling = harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        await harness.TokenExchange.Received(1)
            .RefreshAccessTokenAsync("worker-at-dequeue", Arg.Any<CancellationToken>());
        gate.SetResult();
        await handling;
        Assert.True(completed);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_RunningScan_DoesNotOverwriteNewerWorkerToken()
    {
        const string captured = "worker-captured";
        const string newer = "worker-newer";
        var harness = new Harness { SeedSignIn = false };
        harness.SignInStore.Seed(new HostedSignInRecord
        {
            SchemaVersion = 2,
            OperatorLogin = Login,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            WorkerRefreshToken = captured,
            CredentialUnusable = false,
        });
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());

        var gate = new TaskCompletionSource();
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));
        harness.TokenExchange.RefreshAccessTokenAsync(captured, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var record = harness.SignIns.Single();
                record.WorkerRefreshToken = newer;
                await harness.SignInStore.UpsertAsync(record, "etag", call.Arg<CancellationToken>());
                await Task.Delay(50, call.Arg<CancellationToken>());
                return new HostedGitHubRefreshTokenExchangeResult(
                    "access",
                    "rotated",
                    DateTimeOffset.UtcNow.AddDays(1),
                    null);
            });

        var handling = harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => { }),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => harness.Statuses.Peek(Login)!.Status == AccountScanStatusDocumentConstants.StatusRunning);

        gate.SetResult();
        await handling;

        Assert.Equal(newer, harness.SignIns.Single().WorkerRefreshToken);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_TokenFailureWithNewerToken_SetsFinishedWithoutWait()
    {
        const string captured = "worker-old";
        var harness = new Harness { SeedSignIn = false };
        harness.SignInStore.Seed(new HostedSignInRecord
        {
            SchemaVersion = 2,
            OperatorLogin = Login,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            WorkerRefreshToken = captured,
            CredentialUnusable = false,
        });
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.TokenExchange.RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<HostedGitHubRefreshTokenExchangeResult>>(async call =>
            {
                var storedRecord = harness.SignIns.Single();
                storedRecord.WorkerRefreshToken = "worker-newer";
                await harness.SignInStore.UpsertAsync(storedRecord, "etag", call.Arg<CancellationToken>());
                throw new InvalidOperationException("expired");
            });

        var completed = false;
        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, stored.Status);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultFailure, stored.LastResult);
        Assert.False(harness.SignIns.Single().CredentialUnusable);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_TokenFailureWithoutNewerToken_SetsSuspendedAndWait()
    {
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 2));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.TokenExchange.RefreshAccessTokenAsync("refresh", Arg.Any<CancellationToken>())
            .Returns<Task<HostedGitHubRefreshTokenExchangeResult>>(_ => throw new InvalidOperationException("expired"));
        var completed = false;

        await harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => completed = true),
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var stored = harness.Statuses.Peek(Login)!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusSuspended, stored.Status);
        Assert.True(harness.SignIns.Single().CredentialUnusable);
    }

    [Fact]
    public async Task DailySnapshotCollectionPassService_WebStopDuringRunningPass_DoesNotWriteSiteStopped()
    {
        var gate = new TaskCompletionSource();
        var harness = new Harness();
        harness.Statuses.Seed(Scheduled(generation: 1));
        harness.Snapshots.Seed(Login, SucceededSnapshot());
        harness.Coordinator = CoordinatorTestFactory.CreateWithGithub(CoordinatorTestFactory.Gated(gate));

        var handling = harness.CreateSut().HandleQueueMessageAsync(
            Invocation(Initial(), () => { }),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => harness.Statuses.Peek(Login)!.Status == AccountScanStatusDocumentConstants.StatusRunning);

        // Web has no live task for a worker run; nothing it does may rewrite the pass as stopped.
        var duringRun = harness.Snapshots.GetDocument(Login)!;
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, duringRun.Run.Status);
        Assert.NotEqual(OperatorSnapshotRunReason.SiteStopped, duringRun.Run.Reason);
        Assert.NotEqual(OperatorSnapshotRunReason.ScheduledCollectionStopped, duringRun.Run.Reason);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, harness.Statuses.Peek(Login)!.Status);

        gate.SetResult();
        await handling;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, harness.Statuses.Peek(Login)!.Status);
    }

    private static AccountScanStatusDocument Scheduled(int generation, DateTimeOffset? queueVisibleAfter = null) =>
        AccountScanStatusTestFactory.Document(
            Login,
            AccountScanStatusDocumentConstants.StatusScheduled,
            generation,
            queueMessagePending: true,
            queueVisibleAfter: queueVisibleAfter);

    private static OperatorSnapshotDocument SucceededSnapshot() => new()
    {
        OperatorLogin = Login,
        Revision = 2,
        Trigger = OperatorSnapshotCollectionTrigger.Operator,
        LastSuccessfulCollectionAt = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero),
        Run = new OperatorSnapshotRun
        {
            Status = OperatorSnapshotRunStatus.Succeeded,
            FinishedAt = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero),
        },
    };

    private static DailySnapshotQueueMessage Initial() => new()
    {
        OperatorLogin = Login,
        OccasionDate = "2026-11-23",
        Kind = "initial",
    };

    private static DailySnapshotQueueInvocation Invocation(DailySnapshotQueueMessage message, Action onComplete) => new()
    {
        Message = message,
        CompleteAsync = _ =>
        {
            onComplete();
            return Task.CompletedTask;
        },
        UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Condition was not met in time.");
    }

    private sealed class Harness
    {
        public InMemoryOperatorSnapshotStore Snapshots { get; } = new();

        public InMemoryAccountScanStatusStore Statuses { get; } = new();

        public RecordingDailyQueuePublisher Queue { get; } = new();

        public IHostedGitHubOAuthTokenExchange TokenExchange { get; } = CreateDefaultTokenExchange();

        public IDailyOccasionService? DailyOccasion { get; init; }

        public IDailyCollectionRunnerLease? Runner { get; init; }

        public HostedAdmissionControlOptions? Admission { get; init; }

        public bool SeedSignIn { get; init; } = true;

        public HostedSignInRecord? CustomSignIn { get; init; }

        public DateTimeOffset SignInCompletedAt { get; init; } = DateTimeOffset.UtcNow.AddDays(-1);

        public OperatorSnapshotCollectionCoordinator? Coordinator { get; set; }

        public InMemoryHostedSignInRecordStore SignInStore { get; } = new();

        public IReadOnlyList<HostedSignInRecord> SignIns => SignInStore.ListAsync().GetAwaiter().GetResult();

        public DailySnapshotCollectionPassService CreateSut()
        {
            if (SeedSignIn)
            {
                SignInStore.Seed(CustomSignIn ?? new HostedSignInRecord
                {
                    OperatorLogin = Login,
                    CompletedAt = SignInCompletedAt,
                    SchemaVersion = 2,
                    WorkerRefreshToken = "refresh",
                    CredentialUnusable = false,
                });
            }

            var dailyOccasion = DailyOccasion ?? CreateDefaultDailyOccasion();
            var coordinator = Coordinator ?? new OperatorSnapshotCollectionCoordinator(
                Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
                TimeProvider.System,
                new OperatorSnapshotApplicationStopping(CancellationToken.None),
                NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);

            return new DailySnapshotCollectionPassService(
                new TestScheduledCollectionEnvironment(enabled: true),
                Snapshots,
                SignInStore,
                TokenExchange,
                Queue,
                Runner ?? new AlwaysAvailableRunnerLease(),
                dailyOccasion,
                coordinator,
                new HostedCollectionEligibilityEvaluator(
                    Options.Create(Admission ?? new HostedAdmissionControlOptions { Enabled = false })),
                AccountScanStatusTestFactory.CreateTransitions(Statuses, TimeProvider.System),
                TimeProvider.System,
                NullLogger<DailySnapshotCollectionPassService>.Instance)
            {
                PollInterval = TimeSpan.FromMilliseconds(10),
                LivenessEveryPolls = 1,
            };
        }
    }

    private static IDailyOccasionService CreateDefaultDailyOccasion()
    {
        var dailyOccasion = Substitute.For<IDailyOccasionService>();
        dailyOccasion.TryFillDailyOccasionAsync(Arg.Any<CancellationToken>())
            .Returns(DailyOccasionFillResult.Filled);
        return dailyOccasion;
    }

    private static IHostedGitHubOAuthTokenExchange CreateDefaultTokenExchange()
    {
        var tokenExchange = Substitute.For<IHostedGitHubOAuthTokenExchange>();
        tokenExchange.RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new HostedGitHubRefreshTokenExchangeResult(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddDays(1),
                null));
        return tokenExchange;
    }
}
