namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Publishes messages to the daily snapshot collection queue.</summary>
public interface IDailySnapshotQueuePublisher
{
    /// <summary>Enqueues a message at the tail.</summary>
    Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default);
}
