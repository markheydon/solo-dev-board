namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Publishes messages to the daily snapshot collection queue.</summary>
public interface IDailySnapshotQueuePublisher
{
    /// <summary>Enqueues a message at the tail.</summary>
    Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default);

    /// <summary>Returns true when a visible message for <paramref name="operatorLogin"/> is waiting (peek only).</summary>
    Task<bool> HasVisibleMessageAsync(string operatorLogin, CancellationToken cancellationToken = default);
}
