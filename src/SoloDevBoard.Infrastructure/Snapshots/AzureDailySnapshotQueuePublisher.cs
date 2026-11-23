using System.Text.Json;
using Azure.Storage.Queues;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class AzureDailySnapshotQueuePublisher(QueueServiceClient queueServiceClient) : IDailySnapshotQueuePublisher
{
    private const string QueueName = "daily-snapshot-collection";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default)
    {
        var client = queueServiceClient.GetQueueClient(QueueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await client.SendMessageAsync(payload, cancellationToken).ConfigureAwait(false);
    }
}
