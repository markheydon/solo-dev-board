namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Who started the current snapshot run.</summary>
public enum OperatorSnapshotRunStartedBy
{
    /// <summary>The operator chose Collect snapshot.</summary>
    Operator,

    /// <summary>The daily 05:00 UTC scheduler.</summary>
    DailyOccasion,
}
