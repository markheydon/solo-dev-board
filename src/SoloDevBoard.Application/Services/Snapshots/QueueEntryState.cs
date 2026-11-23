namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>State of an occasion queue entry.</summary>
public enum QueueEntryState
{
    /// <summary>Waiting for the first pass.</summary>
    Waiting,

    /// <summary>Waiting at the back for a further try on the same daily run.</summary>
    RetryWaiting,
}
