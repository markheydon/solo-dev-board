using Azure.Storage.Blobs;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Keyed blob container wrapper for account scan status.</summary>
public sealed class AccountScanStatusBlobContainer(BlobContainerClient client)
{
    /// <summary>Underlying container client.</summary>
    public BlobContainerClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));
}
