namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Where the operator snapshot is kept for display copy.</summary>
public enum OperatorSnapshotDurabilityKind
{
    /// <summary>Local machine file adapter.</summary>
    LocalMachine,

    /// <summary>Container Apps disk file adapter.</summary>
    EphemeralContainerDisk,

    /// <summary>Hosted blob adapter.</summary>
    HostedBlob,
}
