namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Persistence for <c>schedule/occasion.json</c>.</summary>
public interface IDailyOccasionQueueStore
{
    /// <summary>Reads the current queue document, if any.</summary>
    Task<DailyOccasionQueue?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates the document when absent.</summary>
    Task<bool> TryCreateAsync(DailyOccasionQueue queue, CancellationToken cancellationToken = default);

    /// <summary>Conditionally mutates the document.</summary>
    Task<bool> TryUpdateAsync(
        DailyOccasionQueue queue,
        string ifMatchEtag,
        CancellationToken cancellationToken = default);
}
