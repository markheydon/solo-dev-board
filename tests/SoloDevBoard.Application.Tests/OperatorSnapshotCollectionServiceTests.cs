using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotCollectionServiceTests
{
    [Fact]
    public async Task StartAsync_WhenWalkIsLive_ReturnsAlreadyInProgress()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var walkGate = new TaskCompletionSource();
        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns(call => WaitForWalkThenReturnEmptyAsync(walkGate, call.Arg<CancellationToken>()));

        var coordinator = CreateCoordinatorWithGithub(github);
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns("token");
        var service = CreateService(userContext, store, coordinator);

        var first = await service.StartAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OperatorSnapshotStartResult.Started, first);
        Assert.True(coordinator.HasLiveTask("octocat"));

        var second = await service.StartAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OperatorSnapshotStartResult.AlreadyInProgress, second);

        walkGate.SetResult();
        await WaitForCoordinatorIdleAsync(coordinator);

        var document = store.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
    }

    [Fact]
    public async Task StartAsync_UnsafeOwnerLogin_ReturnsUnsafeOperatorLogin()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("../evil");

        var service = CreateService(userContext, new InMemoryOperatorSnapshotStore());

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.UnsafeOperatorLogin, result);
    }

    [Fact]
    public async Task GetStatusAsync_NoDocument_ReturnsNeverRun()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");

        var service = CreateService(userContext, new InMemoryOperatorSnapshotStore());

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.NeverRun, status.Kind);
        Assert.Null(status.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task GetStatusAsync_SucceededWithZeroRepositories_DiffersFromNeverRun()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var finishedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 1,
            LastSuccessfulCollectionAt = finishedAt,
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                StartedAt = finishedAt,
                FinishedAt = finishedAt,
                RepositoriesRecorded = 0,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Succeeded, status.Kind);
        Assert.Equal(0, status.SnapshotRepositoryCount);
        Assert.NotNull(status.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task GetStatusAsync_InterruptedRun_MapsInterruptedKind()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var interruptedAt = DateTimeOffset.Parse("2026-06-01T13:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 3,
            LastSuccessfulCollectionAt = DateTimeOffset.Parse("2026-06-01T10:00:00Z"),
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 1,
                    Owner = "octocat",
                    Name = "demo",
                    Visibility = "public",
                    HtmlUrl = "https://example.com",
                },
            ],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Interrupted,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:30:00Z"),
                FinishedAt = interruptedAt,
                Reason = OperatorSnapshotRunReason.SiteStopped,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Interrupted, status.Kind);
        Assert.Equal(OperatorSnapshotRunReason.SiteStopped, status.Reason);
        Assert.Equal(1, status.SnapshotRepositoryCount);
    }

    [Fact]
    public async Task GetStatusAsync_HostedScheduled_ReportsScheduledWithoutTrigger()
    {
        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document("octocat", AccountScanStatusDocumentConstants.StatusScheduled, 1, true));
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded },
        });

        var service = CreateHostedService(store, statuses, new FixedTimeProvider(HostedNow));

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReportedAccountScanStatus.Scheduled, status.ReportedScanStatus);
        Assert.Null(status.Trigger);
        Assert.Equal(OperatorSnapshotSchedulePosture.Hosted, status.SchedulePosture);
    }

    [Fact]
    public async Task OperatorSnapshotCollectionService_StartWhileScheduledOrRunning_DoesNotStart()
    {
        foreach (var storedStatus in new[]
                 {
                     AccountScanStatusDocumentConstants.StatusScheduled,
                     AccountScanStatusDocumentConstants.StatusRunning,
                 })
        {
            var statuses = new InMemoryAccountScanStatusStore();
            statuses.Seed(AccountScanStatusTestFactory.Document(
                "octocat",
                storedStatus,
                generation: 2,
                queueMessagePending: storedStatus == AccountScanStatusDocumentConstants.StatusScheduled,
                livenessAt: HostedNow,
                runningSince: HostedNow));
            var store = new InMemoryOperatorSnapshotStore();
            var service = CreateHostedService(store, statuses, new FixedTimeProvider(HostedNow));

            var result = await service.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(OperatorSnapshotStartResult.AlreadyInProgress, result);
            Assert.Equal(2, statuses.Peek("octocat")!.ScanGeneration);
            Assert.Equal(storedStatus, statuses.Peek("octocat")!.Status);
            Assert.Null(store.GetDocument("octocat"));
        }
    }

    [Fact]
    public async Task OperatorSnapshotCollectionService_StartWhileStalled_SetsRunningAndLivenessMark()
    {
        var time = new FixedTimeProvider(HostedNow);
        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 3,
            queueMessagePending: true,
            livenessAt: HostedNow.AddMinutes(-6),
            runningSince: HostedNow.AddMinutes(-9)));
        var walkGate = new TaskCompletionSource();
        var github = GatedGitHub(walkGate);
        var service = CreateHostedService(new InMemoryOperatorSnapshotStore(), statuses, time, CreateCoordinatorWithGithub(github));

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.Started, result);
        var stored = statuses.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
        Assert.Equal(4, stored.ScanGeneration);
        Assert.Equal(HostedNow, stored.LivenessAt);
        Assert.Equal(HostedNow, stored.RunningSince);
        Assert.False(stored.QueueMessagePending);
        Assert.Null(stored.SuspensionReason);

        walkGate.SetResult();
        await WaitForStatusAsync(statuses, AccountScanStatusDocumentConstants.StatusFinished);
    }

    [Fact]
    public async Task OperatorSnapshotCollectionService_HandStartedScanPastSixtySeconds_WritesAnotherLivenessMark()
    {
        var time = new FixedTimeProvider(HostedNow);
        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document("octocat", AccountScanStatusDocumentConstants.StatusNeverRun));
        var walkGate = new TaskCompletionSource();
        var service = CreateHostedService(
            new InMemoryOperatorSnapshotStore(),
            statuses,
            time,
            CreateCoordinatorWithGithub(GatedGitHub(walkGate)));

        Assert.Equal(OperatorSnapshotStartResult.Started, await service.StartAsync(TestContext.Current.CancellationToken));
        var generation = statuses.Peek("octocat")!.ScanGeneration;

        time.SetUtcNow(HostedNow.AddSeconds(45));
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (statuses.Peek("octocat")!.LivenessAt == HostedNow && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Equal(HostedNow.AddSeconds(45), statuses.Peek("octocat")!.LivenessAt);
        Assert.Equal(generation, statuses.Peek("octocat")!.ScanGeneration);

        walkGate.SetResult();
        await WaitForStatusAsync(statuses, AccountScanStatusDocumentConstants.StatusFinished);
        var finished = statuses.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultSuccess, finished.LastResult);
        Assert.Null(finished.LivenessAt);
    }

    [Fact]
    public async Task OperatorSnapshotCollectionService_HandStartAndFillRaceOnStalled_ExactlyOneScanStarts()
    {
        var time = new FixedTimeProvider(HostedNow);
        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 1,
            livenessAt: HostedNow.AddMinutes(-10),
            runningSince: HostedNow.AddMinutes(-12)));
        var snapshots = new InMemoryOperatorSnapshotStore();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = "octocat",
            CompletedAt = HostedNow.AddDays(-1),
            SchemaVersion = 2,
            WorkerRefreshToken = "refresh",
        });
        var queue = new RecordingDailyQueuePublisher();
        var fill = DailyOccasionServiceTestFactory.Create(
            time,
            snapshots,
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statuses);
        var walkGate = new TaskCompletionSource();
        var service = CreateHostedService(snapshots, statuses, time, CreateCoordinatorWithGithub(GatedGitHub(walkGate)));

        var handStart = service.StartAsync(TestContext.Current.CancellationToken);
        var fillTask = fill.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);
        var start = await handStart;
        await fillTask;

        var stored = statuses.Peek("octocat")!;
        if (start == OperatorSnapshotStartResult.Started)
        {
            Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, stored.Status);
            Assert.Empty(queue.Messages);
        }
        else
        {
            Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, stored.Status);
            Assert.Single(queue.Messages);
        }

        Assert.Equal(2, stored.ScanGeneration);

        walkGate.SetResult();
    }

    [Fact]
    public async Task OperatorSnapshotCollectionService_StartAsync_MissingWorkerToken_UsesSessionAccessToken()
    {
        const string sessionToken = "session-access-token";
        var signIns = new TrackingHostedSignInRecordStore();
        var store = new InMemoryOperatorSnapshotStore();
        var walkGate = new TaskCompletionSource();
        string? tokenPassed = null;
        var scopeFactory = Substitute.For<ISnapshotCollectionCredentialScopeFactory>();
        scopeFactory.CreateScope(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call =>
            {
                tokenPassed = call.ArgAt<string>(1);
                return new GitHubServiceScope(GatedGitHub(walkGate));
            });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns(sessionToken);

        var coordinator = new OperatorSnapshotCollectionCoordinator(
            scopeFactory,
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);
        var service = new OperatorSnapshotCollectionService(
            userContext,
            store,
            coordinator,
            Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob }),
            new FixedTimeProvider(HostedNow),
            new OperatorSnapshotScheduleFactsService(
                new TestScheduledCollectionEnvironment(enabled: true),
                new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions { Enabled = false }))),
            signIns,
            new TestScheduledCollectionEnvironment(enabled: true),
            AccountScanStatusTestFactory.CreateTransitions(new InMemoryAccountScanStatusStore(), new FixedTimeProvider(HostedNow)),
            NullLogger<OperatorSnapshotCollectionService>.Instance);

        var start = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.Started, start);
        Assert.Equal(sessionToken, tokenPassed);
        Assert.Equal(0, signIns.GetCallCount);

        walkGate.SetResult();
        await WaitForCoordinatorIdleAsync(coordinator);

        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusScheduled,
            generation: 2,
            queueMessagePending: true));
        Assert.Equal(
            OperatorSnapshotStartResult.AlreadyInProgress,
            await CreateHostedService(store, statuses, new FixedTimeProvider(HostedNow))
                .StartAsync(TestContext.Current.CancellationToken));

        statuses.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 3,
            livenessAt: HostedNow,
            runningSince: HostedNow));
        Assert.Equal(
            OperatorSnapshotStartResult.AlreadyInProgress,
            await CreateHostedService(store, statuses, new FixedTimeProvider(HostedNow))
                .StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetStatusAsync_FailedRun_KeepsLastSuccessfulSnapshotMetadata()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var lastSuccess = DateTimeOffset.Parse("2026-05-01T08:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 4,
            LastSuccessfulCollectionAt = lastSuccess,
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 7,
                    Owner = "octocat",
                    Name = "kept",
                    Visibility = "public",
                    HtmlUrl = "https://example.com",
                },
            ],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Failed,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                FinishedAt = DateTimeOffset.Parse("2026-06-01T12:00:30Z"),
                Reason = OperatorSnapshotRunReason.RateLimit,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Failed, status.Kind);
        Assert.Equal(lastSuccess, status.LastSuccessfulCollectionAt);
        Assert.Equal(1, status.SnapshotRepositoryCount);
        Assert.Equal(OperatorSnapshotRunReason.RateLimit, status.Reason);
    }

    private static async Task<IReadOnlyList<Domain.Entities.Repositories.Repository>> WaitForWalkThenReturnEmptyAsync(
        TaskCompletionSource walkGate,
        CancellationToken cancellationToken)
    {
        await walkGate.Task.WaitAsync(cancellationToken);
        return [];
    }

    private static readonly DateTimeOffset HostedNow = new(2026, 11, 25, 6, 0, 0, TimeSpan.Zero);

    private static IGitHubService GatedGitHub(TaskCompletionSource walkGate)
    {
        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns(call => WaitForWalkThenReturnEmptyAsync(walkGate, call.Arg<CancellationToken>()));
        return github;
    }

    private static async Task WaitForStatusAsync(InMemoryAccountScanStatusStore statuses, string expected)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (statuses.Peek("octocat")?.Status == expected)
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail($"Status did not become {expected}.");
    }

    private static OperatorSnapshotCollectionService CreateHostedService(
        IOperatorSnapshotStore store,
        InMemoryAccountScanStatusStore statuses,
        FixedTimeProvider time,
        OperatorSnapshotCollectionCoordinator? coordinator = null)
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns("token");
        coordinator ??= new OperatorSnapshotCollectionCoordinator(
            Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);
        var environment = new TestScheduledCollectionEnvironment(enabled: true);
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = "octocat",
            CompletedAt = HostedNow.AddDays(-1),
            SchemaVersion = 2,
            WorkerRefreshToken = "refresh",
        });

        return new OperatorSnapshotCollectionService(
            userContext,
            store,
            coordinator,
            Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob }),
            time,
            new OperatorSnapshotScheduleFactsService(
                environment,
                new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions { Enabled = false }))),
            signIns,
            environment,
            AccountScanStatusTestFactory.CreateTransitions(statuses, time),
            NullLogger<OperatorSnapshotCollectionService>.Instance)
        {
            MonitorPollInterval = TimeSpan.FromMilliseconds(10),
            LivenessEveryPolls = 1,
        };
    }

    private static OperatorSnapshotCollectionService CreateService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        OperatorSnapshotCollectionCoordinator? coordinator = null)
    {
        coordinator ??= new OperatorSnapshotCollectionCoordinator(
            Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<OperatorSnapshotCollectionCoordinator>>());

        return new OperatorSnapshotCollectionService(
            userContext,
            store,
            coordinator,
            Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine }),
            TimeProvider.System,
            new OperatorSnapshotScheduleFactsService(
                new ScheduledCollectionEnvironment(
                    Options.Create(new GitHubAuthOptions()),
                    Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine })),
                new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions()))),
            new TestHostedSignInRecordStore(),
            new TestScheduledCollectionEnvironment(enabled: false),
            AccountScanStatusTestFactory.CreateTransitions(new InMemoryAccountScanStatusStore(), TimeProvider.System),
            NullLogger<OperatorSnapshotCollectionService>.Instance);
    }

    private static OperatorSnapshotCollectionCoordinator CreateCoordinatorWithGithub(IGitHubService github)
    {
        var scopeFactory = Substitute.For<ISnapshotCollectionCredentialScopeFactory>();
        scopeFactory.CreateScope(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => new GitHubServiceScope(github));

        return new OperatorSnapshotCollectionCoordinator(
            scopeFactory,
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);
    }

    private static async Task WaitForCoordinatorIdleAsync(OperatorSnapshotCollectionCoordinator coordinator)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (!coordinator.HasLiveTask("octocat"))
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.Fail("Coordinator did not finish the walk in time.");
    }

    private sealed class GitHubServiceScope(IGitHubService github) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new GitHubServiceProvider(github);

        public void Dispose()
        {
        }
    }

    private sealed class GitHubServiceProvider(IGitHubService github) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IGitHubService) ? github : null;
    }

    private sealed class TrackingHostedSignInRecordStore : IHostedSignInRecordStore
    {
        public int GetCallCount { get; private set; }

        public Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HostedSignInRecord>>([]);

        public Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult<HostedSignInRecordReadResult>(new HostedSignInRecordReadResult.NotFound());
        }

        public Task<HostedSignInRecordWriteResult> UpsertAsync(
            HostedSignInRecord record,
            string? expectedEtag,
            CancellationToken cancellationToken = default)
            => Task.FromResult<HostedSignInRecordWriteResult>(new HostedSignInRecordWriteResult.Succeeded(string.Empty));
    }

    private sealed class TestHostedSignInRecordStore : IHostedSignInRecordStore
    {
        public Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HostedSignInRecord>>([]);

        public Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
            => Task.FromResult<HostedSignInRecordReadResult>(new HostedSignInRecordReadResult.NotFound());

        public Task<HostedSignInRecordWriteResult> UpsertAsync(
            HostedSignInRecord record,
            string? expectedEtag,
            CancellationToken cancellationToken = default)
            => Task.FromResult<HostedSignInRecordWriteResult>(new HostedSignInRecordWriteResult.Succeeded(string.Empty));
    }
}
