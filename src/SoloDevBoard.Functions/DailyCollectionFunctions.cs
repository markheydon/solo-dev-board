using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Functions;

/// <summary>Thin triggers for scheduled snapshot collection.</summary>
public sealed class DailyCollectionFunctions(
    IDailyOccasionService dailyOccasion,
    IDailySnapshotCollectionPassService passService,
    QueueServiceClient queueServiceClient,
    ILogger<DailyCollectionFunctions> logger)
{
    private const string QueueName = "daily-snapshot-collection";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(DailyOccasionFillTimer))]
    public async Task DailyOccasionFillTimer(
        [TimerTrigger("0 0-15 5 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _ = timerInfo;
        var cancellationToken = context.CancellationToken;
        await dailyOccasion.RepairMissingInitialMessagesAsync(cancellationToken).ConfigureAwait(false);
        var result = await dailyOccasion.TryFillDailyOccasionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Daily occasion fill result: {Result} at {UtcNow}", result, DateTimeOffset.UtcNow);
    }

    [Function(nameof(DailySnapshotQueueConsumer))]
    public async Task DailySnapshotQueueConsumer(
        [QueueTrigger(QueueName, Connection = "operator-snapshot-queues")] QueueMessage queueMessage,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var rawMessage = queueMessage.Body.ToString();
        var message = JsonSerializer.Deserialize<DailySnapshotQueueMessage>(rawMessage, JsonOptions);
        if (message is null)
        {
            logger.LogWarning("Ignored daily snapshot queue message with invalid JSON.");
            return;
        }

        var queueClient = queueServiceClient.GetQueueClient(QueueName);
        var receipt = new QueueReceipt(queueMessage.MessageId, queueMessage.PopReceipt);

        var invocation = new DailySnapshotQueueInvocation
        {
            Message = message,
            CompleteAsync = ct => queueClient.DeleteMessageAsync(receipt.MessageId, receipt.PopReceipt, ct),
            UpdateVisibilityAsync = async (visibility, ct) =>
            {
                var updated = await queueClient
                    .UpdateMessageAsync(receipt.MessageId, receipt.PopReceipt, rawMessage, visibility, ct)
                    .ConfigureAwait(false);
                receipt.PopReceipt = updated.Value.PopReceipt;
            },
        };

        await passService.HandleQueueMessageAsync(invocation, cancellationToken).ConfigureAwait(false);
    }

    private sealed class QueueReceipt(string messageId, string popReceipt)
    {
        public string MessageId { get; } = messageId;

        public string PopReceipt { get; set; } = popReceipt;
    }
}