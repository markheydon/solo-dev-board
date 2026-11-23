namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>One operator on the daily occasion queue.</summary>
public sealed class DailyOccasionQueueEntry
{
    /// <summary>Sanitised operator login.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Queue entry state.</summary>
    public QueueEntryState State { get; set; }

    /// <summary>Completed passes before a further try (rate limit or unexpected).</summary>
    public int AttemptsCompleted { get; set; }
}
