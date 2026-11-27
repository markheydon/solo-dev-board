using Microsoft.Extensions.Logging.Abstractions;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class OperatorSnapshotBlobStoreTests
{
    [Fact]
    public async Task GetAsync_UnsafeLogin_ReturnsUnsafeKey()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());

        var result = await store.GetAsync("../evil", CancellationToken.None);

        Assert.IsType<OperatorSnapshotGetResult.UnsafeKey>(result);
    }

    [Fact]
    public async Task GetAsync_MissingDocument_IsNotFound()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());

        var result = await store.GetAsync("never-run", CancellationToken.None);

        Assert.IsType<OperatorSnapshotGetResult.NotFound>(result);
    }

    [Fact]
    public async Task GetAsync_FailedFirstRun_IsDistinctFromNeverRun()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());

        var failedFirst = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Failed,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = DateTimeOffset.UtcNow,
                Reason = OperatorSnapshotRunReason.Unexpected,
            },
            Repositories = [],
        };

        await store.SaveAsync("octocat", failedFirst, OperatorSnapshotConcurrencyToken.None, CancellationToken.None);
        var result = await store.GetAsync("octocat", CancellationToken.None);

        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(result);
        Assert.Equal(OperatorSnapshotRunStatus.Failed, found.Document.Run.Status);
        Assert.Empty(found.Document.Repositories);
    }

    [Fact]
    public async Task GetAsync_UnsupportedSchema_ReturnsUnsupportedSchema()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());
        var blob = container.GetBlobClient("octocat.json");
        var payload =
            """
            {"schemaVersion":99,"operatorLogin":"octocat","revision":1,"run":{"status":"succeeded"},"repositories":[]}
            """;
        await blob.UploadAsync(
            payload,
            new OperatorSnapshotBlobUploadConditions(null, IfNoneMatchAll: true),
            CancellationToken.None);

        var result = await store.GetAsync("octocat", CancellationToken.None);

        Assert.IsType<OperatorSnapshotGetResult.UnsupportedSchema>(result);
        var download = await blob.DownloadAsync(CancellationToken.None);
        Assert.Equal(payload, download.Content);
    }

    [Fact]
    public async Task SaveAndGet_TwoLogins_AreIsolated()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());

        var alice = BuildDocument("alice", OperatorSnapshotRunStatus.Succeeded);
        var bob = BuildDocument("bob", OperatorSnapshotRunStatus.Succeeded);

        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync("alice", alice, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));
        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync("bob", bob, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));

        var aliceRead = await store.GetAsync("alice", CancellationToken.None);
        var bobRead = await store.GetAsync("bob", CancellationToken.None);

        Assert.IsType<OperatorSnapshotGetResult.Found>(aliceRead);
        Assert.IsType<OperatorSnapshotGetResult.Found>(bobRead);
    }

    [Fact]
    public async Task SaveAsync_StaleToken_ReturnsConcurrencyConflict()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());
        var document = BuildDocument("octocat", OperatorSnapshotRunStatus.Succeeded);

        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync("octocat", document, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));

        var staleAttempt = BuildDocument("octocat", OperatorSnapshotRunStatus.Failed);
        var conflict = await store.SaveAsync("octocat", staleAttempt, OperatorSnapshotConcurrencyToken.None, CancellationToken.None);

        Assert.IsType<OperatorSnapshotSaveResult.ConcurrencyConflict>(conflict);

        var read = await store.GetAsync("octocat", CancellationToken.None);
        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(read);
        Assert.Equal(OperatorSnapshotRunStatus.Succeeded, found.Document.Run.Status);
    }

    [Fact]
    public async Task GetAsync_DailyInProgressWithoutLiveTask_IsNotRewrittenToSiteStopped()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var registry = new TestLiveTaskRegistry();
        var store = CreateStore(container, registry);

        var workerRun = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.Parse("2026-11-23T05:10:00Z"),
            },
        };

        await store.SaveAsync("octocat", workerRun, OperatorSnapshotConcurrencyToken.None, CancellationToken.None);
        var result = await store.GetAsync("octocat", CancellationToken.None);

        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(result);
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, found.Document.Run.Status);
        Assert.Null(found.Document.Run.Reason);
    }

    [Fact]
    public async Task GetAsync_DailyRetryWait_DoesNotBecomeSiteStopped()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = CreateStore(container, new TestLiveTaskRegistry());

        var retryWait = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                Reason = OperatorSnapshotRunReason.RateLimit,
                StartedAt = DateTimeOffset.UtcNow,
            },
        };

        await store.SaveAsync("octocat", retryWait, OperatorSnapshotConcurrencyToken.None, CancellationToken.None);
        var result = await store.GetAsync("octocat", CancellationToken.None);
        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(result);
        Assert.Equal(OperatorSnapshotRunStatus.InProgress, found.Document.Run.Status);
        Assert.Equal(OperatorSnapshotRunReason.RateLimit, found.Document.Run.Reason);
    }

    [Fact]
    public async Task GetAsync_StaleInProgress_WithoutLiveTask_BecomesInterrupted()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var registry = new TestLiveTaskRegistry();
        var store = CreateStore(container, registry);

        var started = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
            },
        };

        await store.SaveAsync("octocat", started, OperatorSnapshotConcurrencyToken.None, CancellationToken.None);
        var result = await store.GetAsync("octocat", CancellationToken.None);

        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(result);
        Assert.Equal(OperatorSnapshotRunStatus.Interrupted, found.Document.Run.Status);
    }

    private static BlobOperatorSnapshotStore CreateStore(
        IOperatorSnapshotBlobContainer container,
        IOperatorSnapshotLiveTaskRegistry registry)
        => new(
            container,
            registry,
            TimeProvider.System,
            NullLogger<BlobOperatorSnapshotStore>.Instance);

    private static OperatorSnapshotDocument BuildDocument(string login, OperatorSnapshotRunStatus status)
        => new()
        {
            OperatorLogin = login,
            LastSuccessfulCollectionAt = status == OperatorSnapshotRunStatus.Succeeded ? DateTimeOffset.UtcNow : null,
            Run = new OperatorSnapshotRun
            {
                Status = status,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = status == OperatorSnapshotRunStatus.InProgress ? null : DateTimeOffset.UtcNow,
            },
            Repositories = status == OperatorSnapshotRunStatus.Succeeded
                ? [new OperatorSnapshotRepositoryEntry { GitHubId = 1, Owner = login, Name = "demo", Visibility = "public", HtmlUrl = "https://example.com" }]
                : [],
        };

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestLiveTaskRegistry : IOperatorSnapshotLiveTaskRegistry
    {
        public bool HasLiveTask(string sanitisedLogin) => false;

        public bool TryGetLiveProgress(string sanitisedLogin, out DateTimeOffset startedAt, out int repositoriesRecorded)
        {
            startedAt = default;
            repositoriesRecorded = 0;
            return false;
        }
    }
}
