namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>GitHub credential kind that produced an inventory run.</summary>
public enum InventoryAccessKind
{
    PersonalAccessToken,
    Installation,
}