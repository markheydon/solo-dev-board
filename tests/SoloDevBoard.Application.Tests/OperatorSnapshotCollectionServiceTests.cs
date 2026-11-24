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
    public async Task GetStatusAsync_QueuedForToday_DoesNotShowInProgress()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            PassExecuting = false,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Failed,
                FinishedAt = null,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.NotEqual(OperatorSnapshotStatusKind.InProgress, status.Kind);
        Assert.Equal(OperatorSnapshotStatusKind.NeverRun, status.Kind);
    }

    [Fact]
    public async Task StartAsync_WhenQueuedOccasionSetForToday_ReturnsAlreadyInProgress()
    {
        var todayOccasion = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = todayOccasion,
            PassExecuting = false,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns("token");
        var service = CreateService(userContext, store);

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.AlreadyInProgress, result);
    }

    [Fact]
    public async Task StartAsync_WhenQueuedOccasionSetForAnotherDay_StartsCollection()
    {
        var staleOccasion = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = staleOccasion,
            PassExecuting = false,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns("token");
        var service = CreateService(userContext, store);

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.Started, result);
    }

    [Fact]
    public async Task StartAsync_WhenDailyRetryWaitInProgress_ReturnsAlreadyInProgress()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            PassExecuting = false,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                Reason = OperatorSnapshotRunReason.RateLimit,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        userContext.GetAccessToken().Returns("token");
        var service = CreateService(userContext, store);

        Assert.Equal(
            OperatorSnapshotStartResult.AlreadyInProgress,
            await service.StartAsync(TestContext.Current.CancellationToken));
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
            new TestHostedSignInRecordStore());
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
