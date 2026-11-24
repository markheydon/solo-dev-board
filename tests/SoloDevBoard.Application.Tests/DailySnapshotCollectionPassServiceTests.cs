using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class DailySnapshotCollectionPassServiceTests
{
    [Fact]
    public async Task HandleQueueMessage_WhenPassExecutingWithActiveLease_RequeuesWithoutInterrupting()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 2,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            PassExecuting = true,
            PassLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(1),
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
            },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue);
        var message = new DailySnapshotQueueMessage { OperatorLogin = "octocat", OccasionDate = "2026-11-23", Kind = "initial" };
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = message,
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var requeued = Assert.Single(queue.Messages);
        Assert.Equal(message.OperatorLogin, requeued.OperatorLogin);
        var document = store.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, document.Run.Status);
        Assert.True(document.PassExecuting);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenUnmetFillVisible_CallsFill()
    {
        var dailyOccasion = Substitute.For<IDailyOccasionService>();
        dailyOccasion.TryFillDailyOccasionAsync(Arg.Any<CancellationToken>())
            .Returns(DailyOccasionFillResult.Filled);

        var completed = false;
        var sut = CreateSut(new InMemoryOperatorSnapshotStore(), dailyOccasion: dailyOccasion);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage { Kind = "unmetFill" },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        await dailyOccasion.Received(1).TryFillDailyOccasionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleQueueMessage_WhenPassExecutingWithExpiredLease_WritesScheduledCollectionStoppedAndUnmetFill()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 2,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            PassExecuting = true,
            PassLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(-1),
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
            },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage { OperatorLogin = "octocat", OccasionDate = "2026-11-23" },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var document = store.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Interrupted, document.Run.Status);
        Assert.Equal(OperatorSnapshotRunReason.ScheduledCollectionStopped, document.Run.Reason);
        Assert.False(document.PassExecuting);
        var unmet = Assert.Single(queue.Messages);
        Assert.Equal("unmetFill", unmet.Kind);
        Assert.False(string.IsNullOrEmpty(unmet.NotBefore));
    }

    [Fact]
    public async Task HandleQueueMessage_WhenOperatorTriggeredInProgress_CompletesWithoutStartingPass()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Operator,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
            },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "initial",
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        Assert.Empty(queue.Messages);
        var document = store.GetDocument("octocat");
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, document!.Run.Status);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenOccasionAlreadyFinished_CompletesWithoutStartingPass()
    {
        var occasionStart = new DateTimeOffset(2026, 11, 23, 5, 30, 0, TimeSpan.Zero);
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            LastSuccessfulCollectionAt = occasionStart,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                FinishedAt = occasionStart,
            },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "initial",
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        Assert.Empty(queue.Messages);
        var document = store.GetDocument("octocat");
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document!.Run.Status);
        Assert.Equal(occasionStart, document.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenSignInOutsideWindowAtDequeue_AttemptsPass()
    {
        var lastSuccess = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            LastSuccessfulCollectionAt = lastSuccess,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                FinishedAt = lastSuccess,
            },
        });

        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = "octocat",
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-40),
            RefreshToken = "refresh",
            CredentialUnusable = false,
        });

        var tokenExchange = CreateDefaultTokenExchange();
        var completed = false;
        var sut = CreateSut(store, signIns: signIns, tokenExchange: tokenExchange);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "initial",
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        await tokenExchange.Received(1).RefreshAccessTokenAsync("refresh", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleQueueMessage_WhenRetryNotBeforeStillAhead_RequeuesWithoutStartingPass()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            QueuedOccasion = null,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                Reason = OperatorSnapshotRunReason.RateLimit,
            },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue);
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(5);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "retry",
                    NotBefore = DailySnapshotRetryNotBefore.ToIso8601Utc(notBefore),
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var requeued = Assert.Single(queue.Messages);
        Assert.Equal("retry", requeued.Kind);
        Assert.Equal(DailySnapshotRetryNotBefore.ToIso8601Utc(notBefore), requeued.NotBefore);
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, store.GetDocument("octocat")!.Run.Status);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenRunRulesFailForRetryWaiting_RestoresRetainedRun()
    {
        var priorSuccess = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            AttemptsUsed = 2,
            RetainedRun = new OperatorSnapshotRetainedRun
            {
                Trigger = OperatorSnapshotCollectionTrigger.Operator,
                Run = new OperatorSnapshotRun
                {
                    Status = OperatorSnapshotRunStatus.Succeeded,
                    FinishedAt = priorSuccess,
                },
            },
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                Reason = OperatorSnapshotRunReason.Unexpected,
            },
        });

        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = "octocat",
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RefreshToken = "refresh",
        });

        var completed = false;
        var sut = CreateSut(
            store,
            signIns: signIns,
            admission: new HostedAdmissionControlOptions
            {
                Enabled = true,
                AllowedUserLogins = "other-user",
            });
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "retry",
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var document = store.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotCollectionTrigger.Operator, document.Trigger);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
        Assert.Null(document.AttemptsUsed);
        Assert.Null(document.RetainedRun);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenSignInRecordMissing_RecordsSignInOrTokenFailure()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });

        var signIns = new InMemoryHostedSignInRecordStore();
        var completed = false;
        var sut = CreateSut(store, signIns: signIns);
        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = new DailySnapshotQueueMessage
                {
                    OperatorLogin = "octocat",
                    OccasionDate = "2026-11-23",
                    Kind = "initial",
                },
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var document = store.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Failed, document.Run.Status);
        Assert.Equal(OperatorSnapshotRunReason.SignInOrToken, document.Run.Reason);
    }

    [Fact]
    public async Task HandleQueueMessage_WhenRunnerLeaseBusy_RequeuesSamePayload()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });

        var queue = new RecordingDailyQueuePublisher();
        var completed = false;
        var sut = CreateSut(store, queue, runner: new NeverAvailableRunnerLease());
        var message = new DailySnapshotQueueMessage
        {
            OperatorLogin = "octocat",
            OccasionDate = "2026-11-23",
            Kind = "initial",
        };

        await sut.HandleQueueMessageAsync(
            new DailySnapshotQueueInvocation
            {
                Message = message,
                CompleteAsync = _ =>
                {
                    completed = true;
                    return Task.CompletedTask;
                },
                UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
            },
            TestContext.Current.CancellationToken);

        Assert.True(completed);
        var requeued = Assert.Single(queue.Messages);
        Assert.Equal(message.OperatorLogin, requeued.OperatorLogin);
        Assert.Equal(message.OccasionDate, requeued.OccasionDate);
    }

    private static DailySnapshotCollectionPassService CreateSut(
        InMemoryOperatorSnapshotStore store,
        RecordingDailyQueuePublisher? queue = null,
        IDailyCollectionRunnerLease? runner = null,
        InMemoryHostedSignInRecordStore? signIns = null,
        HostedAdmissionControlOptions? admission = null,
        IHostedGitHubOAuthTokenExchange? tokenExchange = null,
        IDailyOccasionService? dailyOccasion = null)
    {
        queue ??= new RecordingDailyQueuePublisher();
        if (signIns is null)
        {
            signIns = new InMemoryHostedSignInRecordStore();
            signIns.Seed(new HostedSignInRecord
            {
                OperatorLogin = "octocat",
                CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
                RefreshToken = "refresh",
                CredentialUnusable = false,
            });
        }

        tokenExchange ??= CreateDefaultTokenExchange();

        dailyOccasion ??= CreateDefaultDailyOccasion();

        return new DailySnapshotCollectionPassService(
            new TestScheduledCollectionEnvironment(enabled: true),
            store,
            signIns,
            tokenExchange,
            queue,
            runner ?? new AlwaysAvailableRunnerLease(),
            dailyOccasion,
            new OperatorSnapshotCollectionCoordinator(
                Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
                TimeProvider.System,
                new OperatorSnapshotApplicationStopping(CancellationToken.None),
                NullLogger<OperatorSnapshotCollectionCoordinator>.Instance),
            new HostedCollectionEligibilityEvaluator(
                Options.Create(admission ?? new HostedAdmissionControlOptions { Enabled = false })),
            TimeProvider.System,
            NullLogger<DailySnapshotCollectionPassService>.Instance);
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
