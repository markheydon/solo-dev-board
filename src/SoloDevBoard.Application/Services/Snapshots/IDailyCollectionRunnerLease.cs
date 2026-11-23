namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Blob lease <c>runner</c> in <c>collection-schedule</c> for one pass at a time.</summary>
public interface IDailyCollectionRunnerLease
{
    /// <summary>Tries to acquire the runner lease for a short fill or pass operation.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}
