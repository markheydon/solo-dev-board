using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class BlobAccountScanStatusStoreTests
{
    [Fact]
    public async Task BlobAccountScanStatusStore_LostEtag_DoesNotOverwrite()
    {
        var blob = Substitute.For<BlobClient>();
        blob.UploadAsync(Arg.Any<BinaryData>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response<BlobContentInfo>>>(_ =>
                throw new RequestFailedException(412, "The condition specified using HTTP conditional header(s) is not met."));
        var sut = CreateStore(blob);
        var document = AccountScanStatusDocumentConstants.CreateNeverRun("octocat");

        var result = await sut.TryReplaceAsync(document, "\"stale-etag\"", TestContext.Current.CancellationToken);

        Assert.IsType<AccountScanStatusWriteResult.ConcurrencyConflict>(result);
        await blob.Received(1).UploadAsync(
            Arg.Any<BinaryData>(),
            Arg.Is<BlobUploadOptions>(options => options.Conditions.IfMatch == new ETag("\"stale-etag\"")),
            Arg.Any<CancellationToken>());
        await blob.DidNotReceive().GetPropertiesAsync(
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlobAccountScanStatusStore_SerialisedBlob_ContainsNoRefreshToken()
    {
        string? uploaded = null;
        var blob = Substitute.For<BlobClient>();
        blob.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(false, Substitute.For<Response>())));
        blob.UploadAsync(Arg.Any<BinaryData>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                uploaded = call.Arg<BinaryData>().ToString();
                return Task.FromResult(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(new ETag("\"etag-1\""), DateTimeOffset.UtcNow, null, null, null, null, 0),
                    Substitute.For<Response>()));
            });
        blob.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(
                BlobsModelFactory.BlobProperties(eTag: new ETag("\"etag-1\"")),
                Substitute.For<Response>())));
        var sut = CreateStore(blob);
        var document = AccountScanStatusDocumentConstants.CreateNeverRun("octocat");

        var result = await sut.TryCreateAsync(document, TestContext.Current.CancellationToken);

        Assert.IsType<AccountScanStatusWriteResult.Succeeded>(result);
        Assert.NotNull(uploaded);
        Assert.DoesNotContain("refresh", uploaded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", uploaded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("workerRefreshToken", uploaded, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\":\"neverRun\"", uploaded, StringComparison.Ordinal);
        Assert.Contains("\"scanGeneration\":0", uploaded, StringComparison.Ordinal);
        Assert.Contains("\"lastResult\":null", uploaded, StringComparison.Ordinal);
    }

    private static BlobAccountScanStatusStore CreateStore(BlobClient blob)
    {
        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blob);
        return new BlobAccountScanStatusStore(
            new AccountScanStatusBlobContainer(containerClient),
            NullLogger<BlobAccountScanStatusStore>.Instance);
    }
}
