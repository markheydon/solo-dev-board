namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>One inventory pass over open-source repositories.</summary>
public sealed class InventoryRun
{
    public Guid Id { get; set; }

    public string OperatorKey { get; set; } = string.Empty;

    public InventoryRunTrigger Trigger { get; set; }

    public InventoryAccessKind AccessKind { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? FinishedAtUtc { get; set; }

    public InventoryRunOutcome Outcome { get; set; }

    public bool WasRateLimited { get; set; }

    public string StandardContentHashAtStart { get; set; } = string.Empty;

    public int IntendedRepositoryCount { get; set; }

    public int CheckedRepositoryCount { get; set; }

    public bool IsSuperseded { get; set; }

    public string? FailureReason { get; set; }

    public ICollection<RepositoryInventorySnapshot> Snapshots { get; set; } = [];
}