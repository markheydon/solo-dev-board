using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class DailySnapshotCollectionPassServiceTests
{
    [Fact]
    public async Task HandleQueueMessage_WhenPassExecutingAlreadyTrue_WritesScheduledCollectionStopped()
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

        var completed = false;
        var sut = CreateSut(store);
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
        IDailyCollectionRunnerLease? runner = null)
    {
        queue ??= new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = "octocat",
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RefreshToken = "refresh",
            CredentialUnusable = false,
        });

        var tokenExchange = Substitute.For<IHostedGitHubOAuthTokenExchange>();
        tokenExchange.RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new HostedGitHubRefreshTokenExchangeResult(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddDays(1),
                null));

        return new DailySnapshotCollectionPassService(
            new TestScheduledCollectionEnvironment(enabled: true),
            store,
            signIns,
            tokenExchange,
            queue,
            runner ?? new AlwaysAvailableRunnerLease(),
            new OperatorSnapshotCollectionCoordinator(
                Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
                TimeProvider.System,
                new OperatorSnapshotApplicationStopping(CancellationToken.None),
                NullLogger<OperatorSnapshotCollectionCoordinator>.Instance),
            TimeProvider.System,
            NullLogger<DailySnapshotCollectionPassService>.Instance);
    }
}
