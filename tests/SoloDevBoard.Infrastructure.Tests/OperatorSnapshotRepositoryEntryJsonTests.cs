using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class OperatorSnapshotRepositoryEntryJsonTests
{
    [Fact]
    public async Task SaveAndGet_PreservesPrivateArchivedAndForkFlags()
    {
        var root = Path.Combine(Path.GetTempPath(), "operator-snapshot-json", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = CreateStore(root);

        var entry = new OperatorSnapshotRepositoryEntry
        {
            GitHubId = 42424242,
            Owner = "octocat",
            Name = "private-fork-archived",
            Visibility = "private",
            Archived = true,
            Fork = true,
            HtmlUrl = "https://github.com/octocat/private-fork-archived",
        };

        var document = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            LastSuccessfulCollectionAt = DateTimeOffset.UtcNow,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = DateTimeOffset.UtcNow,
            },
            Repositories = [entry],
        };

        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync("octocat", document, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));

        var read = await store.GetAsync("octocat", CancellationToken.None);
        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(read);
        var roundTrip = Assert.Single(found.Document.Repositories);

        Assert.Equal("private", roundTrip.Visibility);
        Assert.True(roundTrip.Archived);
        Assert.True(roundTrip.Fork);
        Assert.Equal(42424242, roundTrip.GitHubId);

        var json = await File.ReadAllTextAsync(Path.Combine(root, "octocat.json"), CancellationToken.None);
        Assert.Contains("\"visibility\":\"private\"", json, StringComparison.Ordinal);
        Assert.Contains("\"archived\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"fork\":true", json, StringComparison.Ordinal);
    }

    private static FileOperatorSnapshotStore CreateStore(string root)
    {
        var options = Options.Create(new OperatorSnapshotFileStoreOptions { SnapshotDirectory = root });
        return new FileOperatorSnapshotStore(
            options,
            new OperatorSnapshotRepositoryEntryJsonTests.TestLiveTaskRegistry(),
            TimeProvider.System,
            NullLogger<FileOperatorSnapshotStore>.Instance);
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
