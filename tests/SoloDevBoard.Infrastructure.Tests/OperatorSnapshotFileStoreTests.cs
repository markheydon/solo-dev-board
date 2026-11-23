using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class OperatorSnapshotFileStoreTests
{
    [Fact]
    public async Task GetAsync_UnsafeLogin_ReturnsUnsafeKey()
    {
        var root = CreateTempDirectory();
        var store = CreateStore(root, new TestLiveTaskRegistry());

        var result = await store.GetAsync("../evil", CancellationToken.None);

        Assert.IsType<OperatorSnapshotGetResult.UnsafeKey>(result);
    }

    [Fact]
    public async Task SaveAndGet_TwoLogins_AreIsolated()
    {
        var root = CreateTempDirectory();
        var store = CreateStore(root, new TestLiveTaskRegistry());

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
        var aliceDoc = ((OperatorSnapshotGetResult.Found)aliceRead).Document;
        var bobDoc = ((OperatorSnapshotGetResult.Found)bobRead).Document;
        Assert.Equal("alice", aliceDoc.OperatorLogin);
        Assert.Equal("bob", bobDoc.OperatorLogin);
    }

    [Fact]
    public async Task GetAsync_StaleInProgress_WithoutLiveTask_BecomesInterrupted()
    {
        var root = CreateTempDirectory();
        var registry = new TestLiveTaskRegistry();
        var store = CreateStore(root, registry);

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
        Assert.Equal(OperatorSnapshotRunReason.SiteStopped, found.Document.Run.Reason);
    }

    private static FileOperatorSnapshotStore CreateStore(string root, IOperatorSnapshotLiveTaskRegistry registry)
    {
        var options = Options.Create(new OperatorSnapshotFileStoreOptions { SnapshotDirectory = root });
        return new FileOperatorSnapshotStore(
            options,
            registry,
            TimeProvider.System,
            NullLogger<FileOperatorSnapshotStore>.Instance);
    }

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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "operator-snapshot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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
