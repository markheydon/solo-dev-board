using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Azure SDK implementation of <see cref="IOperatorSnapshotBlobContainer"/>.</summary>
public sealed class AzureOperatorSnapshotBlobContainer(BlobContainerClient containerClient) : IOperatorSnapshotBlobContainer
{
    /// <inheritdoc/>
    public IOperatorSnapshotBlobClient GetBlobClient(string blobName)
        => new AzureOperatorSnapshotBlobClient(containerClient.GetBlobClient(blobName));

    private sealed class AzureOperatorSnapshotBlobClient(BlobClient blobClient) : IOperatorSnapshotBlobClient
    {
        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
            => await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

        public async Task<OperatorSnapshotBlobDownload> DownloadAsync(CancellationToken cancellationToken = default)
        {
            var download = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            return new OperatorSnapshotBlobDownload(
                download.Value.Content.ToString(),
                download.Value.Details.ETag.ToString());
        }

        public async Task<string> GetEtagAsync(CancellationToken cancellationToken = default)
        {
            var props = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return props.Value.ETag.ToString();
        }

        public async Task UploadAsync(
            string content,
            OperatorSnapshotBlobUploadConditions conditions,
            CancellationToken cancellationToken = default)
        {
            var requestConditions = new BlobRequestConditions();
            if (conditions.IfNoneMatchAll)
            {
                requestConditions.IfNoneMatch = ETag.All;
            }
            else if (conditions.IfMatch is not null)
            {
                requestConditions.IfMatch = new ETag(conditions.IfMatch);
            }

            await blobClient.UploadAsync(
                    BinaryData.FromString(content),
                    new BlobUploadOptions { Conditions = requestConditions },
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}