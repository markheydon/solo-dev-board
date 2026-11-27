using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class HostedSignInRecordBlobStoreTests
{
    private const string LegacyRefresh = "schema1-browser-refresh-token";

    [Fact]
    public async Task HostedSignInRecord_BlobStore_Schema1Read_LeavesWorkerTokenAbsent()
    {
        var schemaOneJson = """
            {
              "schemaVersion": 1,
              "operatorLogin": "octocat",
              "completedAt": "2026-11-20T08:00:00Z",
              "organisationLogins": ["orgs"],
              "refreshToken": "schema1-browser-refresh-token"
            }
            """;

        var blob = Substitute.For<BlobClient>();
        blob.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(true, Substitute.For<Response>())));
        blob.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(
                BlobsModelFactory.BlobDownloadResult(
                    BinaryData.FromString(schemaOneJson),
                    BlobsModelFactory.BlobDownloadDetails(eTag: new ETag("\"etag-1\""))),
                Substitute.For<Response>())));

        var store = CreateStore(blob);
        var read = await store.GetAsync("octocat", TestContext.Current.CancellationToken);

        var found = Assert.IsType<HostedSignInRecordReadResult.Found>(read);
        Assert.Equal(1, found.Record.SchemaVersion);
        Assert.Equal(LegacyRefresh, found.Record.LegacyRefreshToken);
        Assert.Null(found.Record.WorkerRefreshToken);
        Assert.False(found.Record.HasWorkerRefreshToken);
        Assert.Null(found.Record.WorkerRefreshTokenForExchange);
    }

    private static BlobHostedSignInRecordStore CreateStore(BlobClient blob)
    {
        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blob);
        return new BlobHostedSignInRecordStore(
            new HostedSignInsBlobContainer(containerClient),
            NullLogger<BlobHostedSignInRecordStore>.Instance);
    }
}
