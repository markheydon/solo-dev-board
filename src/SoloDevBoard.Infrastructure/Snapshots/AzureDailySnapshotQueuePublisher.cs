using System.Text.Json;
using Azure.Storage.Queues;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class AzureDailySnapshotQueuePublisher(
    QueueServiceClient queueServiceClient,
    TimeProvider timeProvider) : IDailySnapshotQueuePublisher
{
    private const string QueueName = "daily-snapshot-collection";
    private static readonly TimeSpan MaxVisibility = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc/>
    public async Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default)
    {
        var client = queueServiceClient.GetQueueClient(QueueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var visibility = ComputeInitialVisibility(message);
        await client.SendMessageAsync(payload, visibility, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan? ComputeInitialVisibility(DailySnapshotQueueMessage message)
    {
        if (!string.Equals(message.Kind, "retry", StringComparison.Ordinal)
            || !DailySnapshotRetryNotBefore.TryParseIso8601Utc(message.NotBefore, out var notBefore))
        {
            return null;
        }

        var delay = notBefore - _timeProvider.GetUtcNow();
        if (delay <= TimeSpan.Zero)
        {
            return null;
        }

        return delay > MaxVisibility ? MaxVisibility : delay;
    }
}
