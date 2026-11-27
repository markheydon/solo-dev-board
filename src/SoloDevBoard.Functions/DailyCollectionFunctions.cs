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
    DailyQueueVisibilityRenewer visibilityRenewer,
    QueueServiceClient queueServiceClient,
    ILogger<DailyCollectionFunctions> logger)
{
    private const string QueueName = "daily-snapshot-collection";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(DailyOccasionFillTimer))]
    public async Task DailyOccasionFillTimer(
        [TimerTrigger("0 0 5 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _ = timerInfo;
        var cancellationToken = context.CancellationToken;
        var result = await dailyOccasion.TryFillDailyOccasionAsync(cancellationToken).ConfigureAwait(false);
        if (result == DailyOccasionFillResult.Filled)
        {
            await dailyOccasion.RepairMissingInitialMessagesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Daily occasion fill result: {Result} at {UtcNow}", result, DateTimeOffset.UtcNow);
    }

    [Function(nameof(DailySnapshotQueueConsumer))]
    public async Task DailySnapshotQueueConsumer(
        [QueueTrigger(QueueName, Connection = "operator-snapshot-queues")] QueueMessage queueMessage,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var rawMessage = string.IsNullOrEmpty(queueMessage.MessageText)
            ? queueMessage.Body.ToString()
            : queueMessage.MessageText;
        var message = JsonSerializer.Deserialize<DailySnapshotQueueMessage>(rawMessage, JsonOptions);
        if (message is null)
        {
            throw new InvalidOperationException("Daily snapshot queue message was not valid JSON.");
        }

        var queueClient = queueServiceClient.GetQueueClient(QueueName);
        var receipt = new QueueReceipt(queueMessage.MessageId, queueMessage.PopReceipt);

        // Complete and renew share a gate so a renewal can never change the pop receipt mid-delete.
        using var gate = new SemaphoreSlim(1, 1);
        var completed = false;

        var invocation = new DailySnapshotQueueInvocation
        {
            Message = message,
            CompleteAsync = async ct =>
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    completed = true;
                    await queueClient.DeleteMessageAsync(receipt.MessageId, receipt.PopReceipt, ct).ConfigureAwait(false);
                }
                finally
                {
                    gate.Release();
                }
            },
            UpdateVisibilityAsync = async (visibility, ct) =>
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (completed)
                    {
                        return;
                    }

                    var updated = await queueClient
                        .UpdateMessageAsync(receipt.MessageId, receipt.PopReceipt, rawMessage, visibility, ct)
                        .ConfigureAwait(false);
                    receipt.PopReceipt = updated.Value.PopReceipt;
                }
                finally
                {
                    gate.Release();
                }
            },
        };

        using var renewCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewTask = string.IsNullOrEmpty(message.OperatorLogin)
            ? Task.CompletedTask
            : visibilityRenewer.RunAsync(message.OperatorLogin, invocation.UpdateVisibilityAsync, renewCts.Token);

        try
        {
            await passService.HandleQueueMessageAsync(invocation, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await renewCts.CancelAsync().ConfigureAwait(false);
            await renewTask.ConfigureAwait(false);
        }
    }

    private sealed class QueueReceipt(string messageId, string popReceipt)
    {
        public string MessageId { get; } = messageId;

        public string PopReceipt { get; set; } = popReceipt;
    }
}
