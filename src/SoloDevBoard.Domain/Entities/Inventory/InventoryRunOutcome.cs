namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>Lifecycle outcome for an inventory run.</summary>
public enum InventoryRunOutcome
{
    Queued,
    InProgress,
    Completed,
    Partial,
    Failed,
}