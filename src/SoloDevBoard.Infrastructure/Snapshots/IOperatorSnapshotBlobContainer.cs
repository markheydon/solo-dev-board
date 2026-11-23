namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Blob container access for <see cref="BlobOperatorSnapshotStore"/> (production Azure or test doubles).</summary>
public interface IOperatorSnapshotBlobContainer
{
    /// <summary>Gets a blob client for the named blob.</summary>
    IOperatorSnapshotBlobClient GetBlobClient(string blobName);

    /// <summary>Lists blob names with the given prefix.</summary>
    Task<IReadOnlyList<string>> ListBlobNamesAsync(string prefix, CancellationToken cancellationToken = default);
}

/// <summary>Blob-level operations used by the operator snapshot store.</summary>
public interface IOperatorSnapshotBlobClient
{
    /// <summary>Returns whether the blob exists.</summary>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads blob text content and its entity tag.</summary>
    Task<OperatorSnapshotBlobDownload> DownloadAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the blob entity tag.</summary>
    Task<string> GetEtagAsync(CancellationToken cancellationToken = default);

    /// <summary>Uploads content when <paramref name="conditions"/> allow it.</summary>
    Task UploadAsync(string content, OperatorSnapshotBlobUploadConditions conditions, CancellationToken cancellationToken = default);
}

/// <summary>Downloaded blob payload.</summary>
/// <param name="Content">Serialised document JSON.</param>
/// <param name="Etag">Azure-style entity tag.</param>
public readonly record struct OperatorSnapshotBlobDownload(string Content, string Etag);

/// <summary>Conditional upload rules mirroring Azure blob semantics.</summary>
public readonly record struct OperatorSnapshotBlobUploadConditions(string? IfMatch, bool IfNoneMatchAll);
