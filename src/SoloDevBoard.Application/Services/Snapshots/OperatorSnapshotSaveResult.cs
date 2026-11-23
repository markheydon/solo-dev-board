namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Result of saving a snapshot document.</summary>
public abstract record OperatorSnapshotSaveResult
{
    /// <summary>Save succeeded.</summary>
    public sealed record Succeeded : OperatorSnapshotSaveResult;

    /// <summary>Login could not be used as a storage key.</summary>
    public sealed record UnsafeKey : OperatorSnapshotSaveResult;

    /// <summary>Another writer published a newer revision.</summary>
    public sealed record ConcurrencyConflict : OperatorSnapshotSaveResult;
}
