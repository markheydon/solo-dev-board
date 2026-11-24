namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Held blob lease for exclusive daily fill or pass work.</summary>
public interface IDailyCollectionRunnerLeaseHandle : IAsyncDisposable
{
    /// <summary>Extends the runner lease when supported by the store implementation.</summary>
    Task TryRenewAsync(CancellationToken cancellationToken = default);
}
