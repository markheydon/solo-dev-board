using Azure;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>In-memory blob container for store contract tests.</summary>
public sealed class FakeOperatorSnapshotBlobContainer : IOperatorSnapshotBlobContainer
{
    private readonly Dictionary<string, BlobState> _blobs = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public IOperatorSnapshotBlobClient GetBlobClient(string blobName)
        => new FakeBlobClient(_blobs, blobName);

    private sealed class FakeBlobClient(Dictionary<string, BlobState> blobs, string name) : IOperatorSnapshotBlobClient
    {
        public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(blobs.ContainsKey(name));

        public Task<OperatorSnapshotBlobDownload> DownloadAsync(CancellationToken cancellationToken = default)
        {
            if (!blobs.TryGetValue(name, out var state))
            {
                throw new InvalidOperationException("Blob does not exist.");
            }

            return Task.FromResult(new OperatorSnapshotBlobDownload(state.Content, state.Etag));
        }

        public Task<string> GetEtagAsync(CancellationToken cancellationToken = default)
        {
            if (!blobs.TryGetValue(name, out var state))
            {
                throw new InvalidOperationException("Blob does not exist.");
            }

            return Task.FromResult(state.Etag);
        }

        public Task UploadAsync(
            string content,
            OperatorSnapshotBlobUploadConditions conditions,
            CancellationToken cancellationToken = default)
        {
            var exists = blobs.ContainsKey(name);

            if (conditions.IfNoneMatchAll && exists)
            {
                throw new RequestFailedException(412, "Precondition failed.");
            }

            if (!conditions.IfNoneMatchAll && conditions.IfMatch is not null)
            {
                if (!exists)
                {
                    throw new RequestFailedException(412, "Precondition failed.");
                }

                if (blobs[name].Etag != conditions.IfMatch)
                {
                    throw new RequestFailedException(412, "Precondition failed.");
                }
            }

            var nextEtag = $"\"{Guid.NewGuid():N}\"";
            blobs[name] = new BlobState(content, nextEtag);
            return Task.CompletedTask;
        }
    }

    private sealed record BlobState(string Content, string Etag);
}