namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Options for operator-facing durability copy.</summary>
public sealed class OperatorSnapshotDurabilityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "OperatorSnapshot";

    /// <summary>Where the snapshot is kept for display copy.</summary>
    public OperatorSnapshotDurabilityKind DurabilityKind { get; set; } = OperatorSnapshotDurabilityKind.LocalMachine;
}
