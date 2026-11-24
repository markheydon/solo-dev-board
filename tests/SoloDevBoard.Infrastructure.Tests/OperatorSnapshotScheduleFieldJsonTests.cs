using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class OperatorSnapshotScheduleFieldJsonTests
{
    [Fact]
    public async Task Deserialise_LegacyDocument_AppliesNullScheduleFieldDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), "operator-snapshot-legacy", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = CreateStore(root);

        var legacyJson =
            """
            {"schemaVersion":1,"operatorLogin":"octocat","revision":1,"run":{"status":"succeeded"},"repositories":[]}
            """;
        await File.WriteAllTextAsync(Path.Combine(root, "octocat.json"), legacyJson, TestContext.Current.CancellationToken);

        var read = await store.GetAsync("octocat", TestContext.Current.CancellationToken);
        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(read);

        Assert.Null(found.Document.Trigger);
        Assert.Null(found.Document.QueuedOccasion);
        Assert.False(found.Document.PassExecuting);
        Assert.Null(found.Document.PassLeaseUntil);
        Assert.Null(found.Document.AttemptsUsed);
    }

    [Fact]
    public async Task RoundTrip_ScheduledCollectionStoppedReason_PreservesEnum()
    {
        var root = Path.Combine(Path.GetTempPath(), "operator-snapshot-reason", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = CreateStore(root);

        var document = new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Interrupted,
                Reason = OperatorSnapshotRunReason.ScheduledCollectionStopped,
                FinishedAt = DateTimeOffset.Parse("2026-11-23T06:00:00Z"),
            },
        };

        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync("octocat", document, OperatorSnapshotConcurrencyToken.None, TestContext.Current.CancellationToken));

        var read = await store.GetAsync("octocat", TestContext.Current.CancellationToken);
        var found = Assert.IsType<OperatorSnapshotGetResult.Found>(read);
        Assert.Equal(OperatorSnapshotRunReason.ScheduledCollectionStopped, found.Document.Run.Reason);

        var json = await File.ReadAllTextAsync(Path.Combine(root, "octocat.json"), TestContext.Current.CancellationToken);
        Assert.Contains("scheduledCollectionStopped", json, StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
    }

    private static FileOperatorSnapshotStore CreateStore(string root)
    {
        var options = Options.Create(new OperatorSnapshotFileStoreOptions { SnapshotDirectory = root });
        return new FileOperatorSnapshotStore(
            options,
            new TestLiveTaskRegistry(),
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
