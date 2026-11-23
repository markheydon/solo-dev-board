namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>File adapter path options for operator snapshots.</summary>
public sealed class OperatorSnapshotFileStoreOptions
{
    /// <summary>Directory that contains per-operator JSON files.</summary>
    public string SnapshotDirectory { get; set; } = string.Empty;
}
