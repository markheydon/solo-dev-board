using System.Text.Json;
using Azure.Storage.Queues;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Azure Storage queue publisher for manual inventory scans.</summary>
public sealed class AzureInventoryScanQueue(QueueServiceClient queueServiceClient) : IInventoryScanQueue
{
    public const string QueueName = "inventory-scan-request";

    private readonly QueueServiceClient _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));

    public async Task EnqueueAsync(InventoryScanQueueMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var client = _queueServiceClient.GetQueueClient(QueueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var json = JsonSerializer.Serialize(new
        {
            operatorKey = message.OperatorKey,
            requestedAtUtc = message.RequestedAtUtc,
            requestedByLogin = message.RequestedByLogin,
        });
        await client.SendMessageAsync(json, cancellationToken).ConfigureAwait(false);
    }
}