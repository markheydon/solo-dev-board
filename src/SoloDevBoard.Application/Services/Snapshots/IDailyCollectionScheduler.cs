namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Fills and drains the daily occasion queue.</summary>
public interface IDailyCollectionScheduler
{
    /// <summary>Scheduler constants.</summary>
    public static class Constants
    {
        /// <summary>Occasion hour (UTC).</summary>
        public const int OccasionHourUtc = 5;

        /// <summary>Grace period end minute past the hour (UTC).</summary>
        public const int FillGraceMinuteUtc = 15;

        /// <summary>Background loop interval.</summary>
        public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15);

        /// <summary>Further tries after the first pass.</summary>
        public const int MaxFurtherTries = 3;

        /// <summary>Active pass lease duration.</summary>
        public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

        /// <summary>Sign-in eligibility window.</summary>
        public static readonly TimeSpan SignInWindow = TimeSpan.FromDays(30);
    }

    /// <summary>Runs one fill and drain cycle.</summary>
    /// <param name="replicaLogin">Sanitised login for which this replica holds the lease, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TickAsync(string? replicaLogin, CancellationToken cancellationToken = default);

    /// <summary>Marks an in-flight pass interrupted when the host stops or the lease expires.</summary>
    Task InterruptActivePassAsync(string sanitisedLogin, CancellationToken cancellationToken = default);
}
