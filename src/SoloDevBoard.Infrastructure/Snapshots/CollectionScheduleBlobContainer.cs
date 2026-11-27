using Azure.Storage.Blobs;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Blob container client for collection schedule blobs.</summary>
public sealed class CollectionScheduleBlobContainer(BlobContainerClient client)
{
    /// <summary>Underlying container client.</summary>
    public BlobContainerClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));
}
