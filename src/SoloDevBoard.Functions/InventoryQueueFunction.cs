using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Functions;

/// <summary>Processes manual inventory scan queue messages.</summary>
public sealed class InventoryQueueFunction(InventoryFunctionsWorker worker)
{
    private readonly InventoryFunctionsWorker _worker = worker ?? throw new ArgumentNullException(nameof(worker));

    [Function(nameof(InventoryQueueFunction))]
    public async Task Process(
        [QueueTrigger("inventory-scan-request", Connection = "ConnectionStrings:storage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(rawMessage);
        var operatorKey = document.RootElement.GetProperty("operatorKey").GetString()
            ?? throw new InvalidOperationException("Queue message is missing operatorKey.");
        await _worker.RunForOperatorKeyAsync(operatorKey, InventoryRunTrigger.Manual, cancellationToken).ConfigureAwait(false);
    }
}