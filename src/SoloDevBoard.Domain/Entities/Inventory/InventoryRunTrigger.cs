namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>How an inventory run was started.</summary>
public enum InventoryRunTrigger
{
    Scheduled,
    Manual,
}