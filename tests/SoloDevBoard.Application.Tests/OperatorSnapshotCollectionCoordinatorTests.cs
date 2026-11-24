using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotCollectionCoordinatorTests
{
    private const string Login = "octocat";

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, OperatorSnapshotRunReason.SignInOrToken)]
    [InlineData(HttpStatusCode.TooManyRequests, OperatorSnapshotRunReason.RateLimit)]
    public async Task Walk_GitHubFailure_KeepsLastSuccessfulSnapshot(
        HttpStatusCode statusCode,
        OperatorSnapshotRunReason expectedReason)
    {
        var store = new InMemoryOperatorSnapshotStore();
        SeedInProgressWithPriorSuccess(store);
        var coordinator = CreateCoordinator(store, CreateFailingGitHub(statusCode));

        var started = coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct));

        Assert.True(started);
        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Failed, document.Run.Status);
        Assert.Equal(expectedReason, document.Run.Reason);
        Assert.Single(document.Repositories);
        Assert.Equal(99, document.Repositories[0].GitHubId);
        Assert.NotNull(document.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task Walk_Success_WithDailyTrigger_RecordsSameRepositoryFactsAsHandStart()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Revision = 5,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            AttemptsUsed = 1,
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                RepositoriesRecorded = 0,
            },
        });

        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns(
            [
                new Domain.Entities.Repositories.Repository
                {
                    GitHubId = 42,
                    OwnerLogin = Login,
                    Name = "catalogue",
                    FullName = $"{Login}/catalogue",
                    Url = "https://example.com/octocat/catalogue",
                    IsPrivate = false,
                },
            ]);

        var coordinator = CreateCoordinator(store, github);
        Assert.True(coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct)));

        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
        var entry = Assert.Single(document.Repositories);
        Assert.Equal(42, entry.GitHubId);
        Assert.Equal("catalogue", entry.Name);
        Assert.Equal("public", entry.Visibility);
    }

    [Fact]
    public async Task Walk_Success_PreservesLaterFacts()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var lastSuccess = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        store.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Revision = 5,
            LastSuccessfulCollectionAt = lastSuccess,
            LaterFacts = new Dictionary<string, object?> { ["extension"] = "keep-me" },
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-01-02T10:00:00Z"),
                RepositoriesRecorded = 0,
            },
        });

        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns([]);

        var coordinator = CreateCoordinator(store, github);

        Assert.True(coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct)));

        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, document.Run.Status);
        Assert.Equal("keep-me", document.LaterFacts["extension"]);
    }

    [Fact]
    public async Task Walk_DailyRateLimit_KeepsInProgressForRetry()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Revision = 5,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            PassExecuting = true,
            AttemptsUsed = 1,
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                RepositoriesRecorded = 0,
            },
        });

        var coordinator = CreateCoordinator(store, CreateFailingGitHub(HttpStatusCode.TooManyRequests));

        Assert.True(coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct)));

        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, document.Run.Status);
        Assert.False(document.PassExecuting);
        Assert.Equal(OperatorSnapshotRunReason.RateLimit, document.Run.Reason);
        Assert.Null(document.Run.FinishedAt);
    }

    [Fact]
    public async Task Walk_DailyShutdown_ClearsPassExecutingAndRecordsScheduledStop()
    {
        using var stoppingCts = new CancellationTokenSource();
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Revision = 5,
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            PassExecuting = true,
            PassLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(5),
            QueuedOccasion = "2026-11-23",
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                RepositoriesRecorded = 0,
            },
        });

        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns(async callInfo =>
            {
                await Task.Delay(Timeout.Infinite, callInfo.Arg<CancellationToken>()).ConfigureAwait(false);
                return [];
            });

        var scopeFactory = Substitute.For<ISnapshotCollectionCredentialScopeFactory>();
        scopeFactory.CreateScope(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => new GitHubServiceScope(github));

        var coordinator = new OperatorSnapshotCollectionCoordinator(
            scopeFactory,
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(stoppingCts.Token),
            NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);

        Assert.True(coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct)));

        await Task.Delay(50, TestContext.Current.CancellationToken);
        stoppingCts.Cancel();
        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunStatus.Interrupted, document.Run.Status);
        Assert.Equal(OperatorSnapshotRunReason.ScheduledCollectionStopped, document.Run.Reason);
        Assert.False(document.PassExecuting);
        Assert.Null(document.PassLeaseUntil);
        Assert.Null(document.QueuedOccasion);
    }

    [Fact]
    public async Task Walk_UnexpectedFailure_KeepsPriorRepositories()
    {
        var store = new InMemoryOperatorSnapshotStore();
        SeedInProgressWithPriorSuccess(store);
        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var coordinator = CreateCoordinator(store, github);

        Assert.True(coordinator.TryBegin(
            Login,
            Login,
            "token",
            new OperatorSnapshotConcurrencyToken(5, null),
            (login, ct) => store.GetAsync(login, ct),
            (login, doc, token, ct) => SaveAsync(store, login, doc, token, ct)));

        await WaitForIdleAsync(coordinator);

        var document = store.GetDocument(Login);
        Assert.NotNull(document);
        Assert.Equal(OperatorSnapshotRunReason.Unexpected, document.Run.Reason);
        Assert.Single(document.Repositories);
    }

    private static void SeedInProgressWithPriorSuccess(InMemoryOperatorSnapshotStore store)
    {
        var lastSuccess = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        store.Seed(Login, new OperatorSnapshotDocument
        {
            OperatorLogin = Login,
            Revision = 5,
            LastSuccessfulCollectionAt = lastSuccess,
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 99,
                    Owner = Login,
                    Name = "kept",
                    Visibility = "public",
                    HtmlUrl = "https://example.com/octocat/kept",
                },
            ],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                RepositoriesRecorded = 0,
            },
        });
    }

    private static IGitHubService CreateFailingGitHub(HttpStatusCode statusCode)
    {
        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .ThrowsAsync(new HttpRequestException("failure", inner: null, statusCode: statusCode));
        return github;
    }

    private static OperatorSnapshotCollectionCoordinator CreateCoordinator(
        InMemoryOperatorSnapshotStore store,
        IGitHubService github)
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

    private static async Task<bool> SaveAsync(
        InMemoryOperatorSnapshotStore store,
        string login,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        CancellationToken cancellationToken)
    {
        var result = await store.SaveAsync(login, document, token, cancellationToken);
        return result is OperatorSnapshotSaveResult.Succeeded;
    }

    private static async Task WaitForIdleAsync(OperatorSnapshotCollectionCoordinator coordinator)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (!coordinator.HasLiveTask(Login))
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
}
