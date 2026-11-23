namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Fills the daily collection queue during the 05:00–05:15 UTC window.</summary>
public interface IDailyOccasionService
{
    /// <summary>
    /// Attempts to create today's occasion marker and enqueue eligible operators once.
    /// </summary>
    Task<DailyOccasionFillResult> TryFillDailyOccasionAsync(CancellationToken cancellationToken = default);

    /// <summary>Enqueues missing <c>initial</c> messages for today's marker.</summary>
    Task RepairMissingInitialMessagesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a daily occasion fill attempt.</summary>
public enum DailyOccasionFillResult
{
    /// <summary>Outside the fill window or lean posture.</summary>
    Skipped,

    /// <summary>Today's occasion was already filled.</summary>
    AlreadyFilled,

    /// <summary>Today's queue was filled.</summary>
    Filled,

    /// <summary>Another copy won the fill lease.</summary>
    LostRace,
}
