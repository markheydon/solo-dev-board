namespace SoloDevBoard.Application.Authentication;

/// <summary>Reads and writes durable hosted sign-in blobs.</summary>
public interface IHostedSignInRecordStore
{
    /// <summary>Lists all stored sign-in records.</summary>
    Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one record, or null when absent.</summary>
    Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default);

    /// <summary>Creates or replaces a record.</summary>
    Task<HostedSignInRecordWriteResult> UpsertAsync(
        HostedSignInRecord record,
        string? expectedEtag,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of reading a hosted sign-in record.</summary>
public abstract record HostedSignInRecordReadResult
{
    /// <summary>Record found.</summary>
    public sealed record Found(HostedSignInRecord Record, string Etag) : HostedSignInRecordReadResult;

    /// <summary>No blob exists.</summary>
    public sealed record NotFound : HostedSignInRecordReadResult;

    /// <summary>Login could not be stored safely.</summary>
    public sealed record UnsafeKey : HostedSignInRecordReadResult;
}

/// <summary>Result of writing a hosted sign-in record.</summary>
public abstract record HostedSignInRecordWriteResult
{
    /// <summary>Write succeeded.</summary>
    public sealed record Succeeded(string Etag) : HostedSignInRecordWriteResult;

    /// <summary>Etag mismatch.</summary>
    public sealed record ConcurrencyConflict : HostedSignInRecordWriteResult;

    /// <summary>Login could not be stored safely.</summary>
    public sealed record UnsafeKey : HostedSignInRecordWriteResult;
}
