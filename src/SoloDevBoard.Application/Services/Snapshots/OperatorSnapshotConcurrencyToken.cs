namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Token for conditional snapshot writes.</summary>
/// <param name="Revision">Expected document revision.</param>
/// <param name="BlobEtag">Azure blob etag when using the blob adapter; otherwise null.</param>
public readonly record struct OperatorSnapshotConcurrencyToken(long Revision, string? BlobEtag)
{
    /// <summary>Token used when creating a new document.</summary>
    public static OperatorSnapshotConcurrencyToken None => new(0, null);
}
