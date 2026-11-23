namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Stores one JSON snapshot document per operator.</summary>
public interface IOperatorSnapshotStore
{
    /// <summary>Reads the snapshot for <paramref name="operatorLogin"/>.</summary>
    Task<OperatorSnapshotGetResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default);

    /// <summary>Conditionally saves <paramref name="document"/> for <paramref name="operatorLogin"/>.</summary>
    Task<OperatorSnapshotSaveResult> SaveAsync(
        string operatorLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken expectedToken,
        CancellationToken cancellationToken = default);
}
