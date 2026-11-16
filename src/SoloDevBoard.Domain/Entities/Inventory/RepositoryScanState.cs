namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>Persisted scan state on a repository snapshot row.</summary>
public enum RepositoryScanState
{
    Checked,
    NotScanned,
    NotVisible,
}