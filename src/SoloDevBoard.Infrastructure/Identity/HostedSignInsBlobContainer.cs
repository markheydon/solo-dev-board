using Azure.Storage.Blobs;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Blob container client for hosted sign-in records.</summary>
public sealed class HostedSignInsBlobContainer(BlobContainerClient client)
{
    /// <summary>Underlying container client.</summary>
    public BlobContainerClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));
}
