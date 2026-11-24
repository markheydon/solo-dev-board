using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class BlobDailyCollectionRunnerLease(CollectionScheduleBlobContainer container) : IDailyCollectionRunnerLease
{
    /// <inheritdoc/>
    public async Task<IAsyncDisposable?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var blob = container.Client.GetBlobClient("runner");
        var leaseClient = blob.GetBlobLeaseClient();
        try
        {
            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                await blob.UploadAsync(
                        BinaryData.Empty,
                        new BlobUploadOptions { Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All } },
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new ReleaseOnDispose(leaseClient);
        }
        catch (RequestFailedException)
        {
            return null;
        }
    }

    private sealed class ReleaseOnDispose(BlobLeaseClient leaseClient) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await leaseClient.ReleaseAsync().ConfigureAwait(false);
            }
            catch (Azure.RequestFailedException)
            {
                // Best effort release.
            }
        }
    }
}
