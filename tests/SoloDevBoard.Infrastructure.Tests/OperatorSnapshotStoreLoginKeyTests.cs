using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Store keys follow sanitised owner logins for hosted claim and PAT paths.</summary>
public sealed class OperatorSnapshotStoreLoginKeyTests
{
    [Fact]
    public async Task SaveAndGet_DistinctSanitisedLogins_UseDistinctFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "operator-snapshot-keys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = CreateStore(root);

        const string hostedClaimLogin = "hosted-operator";
        const string patResolvedLogin = "pat-resolved-operator";

        var hostedDocument = BuildDocument(hostedClaimLogin, repositoryName: "hosted-repo");
        var patDocument = BuildDocument(patResolvedLogin, repositoryName: "pat-repo");

        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync(hostedClaimLogin, hostedDocument, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));
        Assert.IsType<OperatorSnapshotSaveResult.Succeeded>(
            await store.SaveAsync(patResolvedLogin, patDocument, OperatorSnapshotConcurrencyToken.None, CancellationToken.None));

        var hostedPath = Path.Combine(root, "hosted-operator.json");
        var patPath = Path.Combine(root, "pat-resolved-operator.json");
        Assert.True(File.Exists(hostedPath));
        Assert.True(File.Exists(patPath));

        var crossReadHosted = await store.GetAsync(hostedClaimLogin, CancellationToken.None);
        var crossReadPat = await store.GetAsync(patResolvedLogin, CancellationToken.None);
        var hosted = Assert.IsType<OperatorSnapshotGetResult.Found>(crossReadHosted).Document;
        var pat = Assert.IsType<OperatorSnapshotGetResult.Found>(crossReadPat).Document;

        Assert.Equal("hosted-repo", hosted.Repositories[0].Name);
        Assert.Equal("pat-repo", pat.Repositories[0].Name);
        Assert.DoesNotContain("pat-repo", await File.ReadAllTextAsync(hostedPath, CancellationToken.None), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAndGet_TwoLogins_CannotReadOtherOperatorsSnapshot()
    {
        var root = Path.Combine(Path.GetTempPath(), "operator-snapshot-keys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = CreateStore(root);

        await store.SaveAsync(
            "operator-a",
            BuildDocument("operator-a", repositoryName: "a-only"),
            OperatorSnapshotConcurrencyToken.None,
            CancellationToken.None);
        await store.SaveAsync(
            "operator-b",
            BuildDocument("operator-b", repositoryName: "b-only"),
            OperatorSnapshotConcurrencyToken.None,
            CancellationToken.None);

        var aRead = await store.GetAsync("operator-a", CancellationToken.None);
        var bRead = await store.GetAsync("operator-b", CancellationToken.None);

        var aDoc = Assert.IsType<OperatorSnapshotGetResult.Found>(aRead).Document;
        var bDoc = Assert.IsType<OperatorSnapshotGetResult.Found>(bRead).Document;
        Assert.Equal("a-only", aDoc.Repositories[0].Name);
        Assert.Equal("b-only", bDoc.Repositories[0].Name);
        Assert.Equal("operator-a", aDoc.OperatorLogin);
        Assert.Equal("operator-b", bDoc.OperatorLogin);
    }

    private static OperatorSnapshotDocument BuildDocument(string login, string repositoryName)
        => new()
        {
            OperatorLogin = login,
            LastSuccessfulCollectionAt = DateTimeOffset.UtcNow,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = DateTimeOffset.UtcNow,
            },
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 1,
                    Owner = login,
                    Name = repositoryName,
                    Visibility = "public",
                    HtmlUrl = "https://example.com",
                },
            ],
        };

    private static FileOperatorSnapshotStore CreateStore(string root)
    {
        var options = Options.Create(new OperatorSnapshotFileStoreOptions { SnapshotDirectory = root });
        return new FileOperatorSnapshotStore(
            options,
            new OperatorSnapshotStoreLoginKeyTests.TestLiveTaskRegistry(),
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
