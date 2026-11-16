using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Shared entry for timer and queue inventory walks.</summary>
public interface IInventoryScanOrchestrator
{
    Task RunAsync(string operatorKey, InventoryRunTrigger trigger, CancellationToken cancellationToken);
}