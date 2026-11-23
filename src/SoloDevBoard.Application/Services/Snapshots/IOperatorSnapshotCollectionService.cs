namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Starts and reads operator snapshot collection for the current user.</summary>
public interface IOperatorSnapshotCollectionService
{
    /// <summary>Starts a collection walk if none is already in progress.</summary>
    Task<OperatorSnapshotStartResult> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads collection status for the current user.</summary>
    Task<OperatorSnapshotStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
