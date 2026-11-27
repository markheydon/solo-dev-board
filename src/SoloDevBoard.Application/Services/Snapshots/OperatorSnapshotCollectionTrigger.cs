namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>What started the current or last operator snapshot run.</summary>
public enum OperatorSnapshotCollectionTrigger
{
    /// <summary>The operator started the collection from the Repositories page.</summary>
    Operator,

    /// <summary>The scheduled daily occasion started the collection.</summary>
    Daily,
}
