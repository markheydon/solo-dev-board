using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class BlobDailyCollectionRunnerLease(CollectionScheduleBlobContainer container) : IDailyCollectionRunnerLease
{
    /// <inheritdoc/>
    private static readonly TimeSpan MaxBlobLeaseDuration = TimeSpan.FromSeconds(60);

    /// <inheritdoc/>
    public async Task<IDailyCollectionRunnerLeaseHandle?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var blob = container.Client.GetBlobClient("runner");
        var leaseClient = blob.GetBlobLeaseClient();
        var leaseDuration = duration > MaxBlobLeaseDuration ? MaxBlobLeaseDuration : duration;
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

            await leaseClient.AcquireAsync(leaseDuration, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new RunnerLeaseHandle(leaseClient);
        }
        catch (RequestFailedException)
        {
            return null;
        }
    }

    private sealed class RunnerLeaseHandle(BlobLeaseClient leaseClient) : IDailyCollectionRunnerLeaseHandle
    {
        public async Task TryRenewAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await leaseClient.RenewAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException)
            {
                // Best effort renew.
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await leaseClient.ReleaseAsync().ConfigureAwait(false);
            }
            catch (RequestFailedException)
            {
                // Best effort release.
            }
        }
    }
}
