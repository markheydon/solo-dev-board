using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Web.Snapshots;

/// <summary>Hosted background loop that fills and drains the daily occasion queue.</summary>
public sealed class DailyCollectionBackgroundService(
    IDailyCollectionScheduler scheduler,
    OperatorSnapshotApplicationStopping applicationStopping,
    TimeProvider timeProvider,
    ILogger<DailyCollectionBackgroundService> logger) : BackgroundService
{
    private readonly IDailyCollectionScheduler _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    private readonly OperatorSnapshotApplicationStopping _applicationStopping = applicationStopping ?? throw new ArgumentNullException(nameof(applicationStopping));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<DailyCollectionBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IDailyCollectionScheduler.Constants.TickInterval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await _scheduler.TickAsync(replicaLogin: null, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Daily collection scheduler tick failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Active pass interruption is handled when the lease expires or the host stops mid-pass.
        }
    }
}
