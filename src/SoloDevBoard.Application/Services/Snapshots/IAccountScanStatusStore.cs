namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Reads and writes account scan status blobs.</summary>
public interface IAccountScanStatusStore
{
    /// <summary>Gets one record, or not found when absent.</summary>
    Task<AccountScanStatusReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default);

    /// <summary>Creates the blob when absent.</summary>
    Task<AccountScanStatusWriteResult> TryCreateAsync(
        AccountScanStatusDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the blob under an etag.</summary>
    Task<AccountScanStatusWriteResult> TryReplaceAsync(
        AccountScanStatusDocument document,
        string expectedEtag,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of reading account scan status.</summary>
public abstract record AccountScanStatusReadResult
{
    /// <summary>Record found.</summary>
    public sealed record Found(AccountScanStatusDocument Document, string Etag) : AccountScanStatusReadResult;

    /// <summary>No blob exists.</summary>
    public sealed record NotFound : AccountScanStatusReadResult;

    /// <summary>Login could not be stored safely.</summary>
    public sealed record UnsafeKey : AccountScanStatusReadResult;
}

/// <summary>Result of writing account scan status.</summary>
public abstract record AccountScanStatusWriteResult
{
    /// <summary>Write succeeded.</summary>
    public sealed record Succeeded(string Etag) : AccountScanStatusWriteResult;

    /// <summary>Etag mismatch.</summary>
    public sealed record ConcurrencyConflict : AccountScanStatusWriteResult;

    /// <summary>Blob already exists on create.</summary>
    public sealed record AlreadyExists : AccountScanStatusWriteResult;

    /// <summary>Login could not be stored safely.</summary>
    public sealed record UnsafeKey : AccountScanStatusWriteResult;
}
