using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Inventory run and snapshot persistence.</summary>
public interface IInventoryRunRepository
{
    Task<InventoryRun?> GetActiveRunAsync(string operatorKey, CancellationToken cancellationToken);

    Task<InventoryRun?> GetLatestFinishedRunAsync(string operatorKey, CancellationToken cancellationToken);

    Task<InventoryRun?> GetLatestAttemptAsync(string operatorKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<RepositoryInventorySnapshot>> GetSnapshotsForRunAsync(Guid runId, CancellationToken cancellationToken);

    Task AddRunAsync(InventoryRun run, CancellationToken cancellationToken);

    Task UpdateRunAsync(InventoryRun run, CancellationToken cancellationToken);

    Task AddSnapshotsAsync(IEnumerable<RepositoryInventorySnapshot> snapshots, CancellationToken cancellationToken);

    Task SupersedeFinishedRunsAsync(string operatorKey, Guid exceptRunId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryRun>> GetStuckRunsAsync(TimeSpan stuckThreshold, CancellationToken cancellationToken);
}