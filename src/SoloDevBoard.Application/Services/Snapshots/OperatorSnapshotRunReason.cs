namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Short reasons stored on a failed or interrupted run.</summary>
public enum OperatorSnapshotRunReason
{
    /// <summary>Sign-in or token problem.</summary>
    SignInOrToken,

    /// <summary>GitHub rate limit.</summary>
    RateLimit,

    /// <summary>The site stopped before the collection finished.</summary>
    SiteStopped,

    /// <summary>The scheduled collection process stopped before a daily pass finished.</summary>
    ScheduledCollectionStopped,

    /// <summary>Unexpected error.</summary>
    Unexpected,
}
