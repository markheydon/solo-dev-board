namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Lean posture stub with no occasion queue.</summary>
public sealed class NoOpDailyOccasionQueueStore : IDailyOccasionQueueStore
{
    public Task<DailyOccasionQueue?> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<DailyOccasionQueue?>(null);

    public Task<bool> TryCreateAsync(DailyOccasionQueue queue, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> TryUpdateAsync(DailyOccasionQueue queue, string ifMatchEtag, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
