namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Enqueue manual inventory scan work.</summary>
public interface IInventoryScanQueue
{
    Task EnqueueAsync(InventoryScanQueueMessage message, CancellationToken cancellationToken);
}

/// <summary>Queue payload for manual scan requests.</summary>
public sealed record InventoryScanQueueMessage(
    string OperatorKey,
    DateTimeOffset RequestedAtUtc,
    string? RequestedByLogin);