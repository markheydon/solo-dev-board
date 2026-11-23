using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Functions;

/// <summary>Thin triggers for scheduled snapshot collection.</summary>
public sealed class DailyCollectionFunctions(
    IDailyOccasionService dailyOccasion,
    IDailySnapshotCollectionPassService passService,
    ILogger<DailyCollectionFunctions> logger)
{
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
        [QueueTrigger("daily-snapshot-collection", Connection = "operator-snapshot-queues")] string rawMessage,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var message = JsonSerializer.Deserialize<DailySnapshotQueueMessage>(rawMessage, JsonOptions);
        if (message is null)
        {
            logger.LogWarning("Ignored daily snapshot queue message with invalid JSON.");
            return;
        }

        var invocation = new DailySnapshotQueueInvocation
        {
            Message = message,
            CompleteAsync = _ => Task.CompletedTask,
            UpdateVisibilityAsync = (_, _) => Task.CompletedTask,
        };

        await passService.HandleQueueMessageAsync(invocation, cancellationToken).ConfigureAwait(false);
    }
}
