namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Result of reading a snapshot document.</summary>
public abstract record OperatorSnapshotGetResult
{
    /// <summary>Document was not found.</summary>
    public sealed record NotFound : OperatorSnapshotGetResult;

    /// <summary>Login could not be used as a storage key.</summary>
    public sealed record UnsafeKey : OperatorSnapshotGetResult;

    /// <summary>Document could not be understood; body was left unchanged.</summary>
    public sealed record UnsupportedSchema : OperatorSnapshotGetResult;

    /// <summary>Document was read successfully.</summary>
    /// <param name="Document">The snapshot.</param>
    /// <param name="Token">Concurrency token for the next write.</param>
    public sealed record Found(OperatorSnapshotDocument Document, OperatorSnapshotConcurrencyToken Token) : OperatorSnapshotGetResult;
}
