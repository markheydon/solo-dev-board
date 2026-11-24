using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>No-op schedule storage for lean posture.</summary>
public sealed class NullCollectionOccasionMarkerStore : ICollectionOccasionMarkerStore
{
    /// <inheritdoc/>
    public Task<OccasionMarkerReadResult> GetAsync(string occasionDate, CancellationToken cancellationToken = default)
        => Task.FromResult<OccasionMarkerReadResult>(new OccasionMarkerReadResult.NotFound());

    /// <inheritdoc/>
    public Task<OccasionMarkerWriteResult> TryCreateAsync(OccasionMarker marker, CancellationToken cancellationToken = default)
        => Task.FromResult<OccasionMarkerWriteResult>(new OccasionMarkerWriteResult.AlreadyExists());
}

/// <summary>No-op queue for lean posture.</summary>
public sealed class NullDailySnapshotQueuePublisher : IDailySnapshotQueuePublisher
{
    /// <inheritdoc/>
    public Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>No-op runner lease for lean posture.</summary>
public sealed class NullDailyCollectionRunnerLease : IDailyCollectionRunnerLease
{
    /// <inheritdoc/>
    public Task<IDailyCollectionRunnerLeaseHandle?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.FromResult<IDailyCollectionRunnerLeaseHandle?>(null);
}
