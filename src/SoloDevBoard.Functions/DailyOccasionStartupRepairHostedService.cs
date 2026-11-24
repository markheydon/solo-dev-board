using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Functions;

/// <summary>Repairs queue messages and runs catch-up fill when the worker starts after 05:00 UTC with no marker.</summary>
internal sealed class DailyOccasionStartupRepairHostedService(
    IDailyOccasionService dailyOccasion,
    ICollectionOccasionMarkerStore markerStore,
    TimeProvider timeProvider,
    ILogger<DailyOccasionStartupRepairHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dailyOccasion.RepairMissingInitialMessagesAsync(cancellationToken).ConfigureAwait(false);

            var utcNow = timeProvider.GetUtcNow();
            var todayOccasion = GetTodayOccasionAt(utcNow);
            if (utcNow < todayOccasion)
            {
                logger.LogInformation("Daily occasion startup repair completed; before 05:00 UTC, no catch-up fill.");
                return;
            }

            var occasionDate = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var marker = await markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
            if (marker is OccasionMarkerReadResult.NotFound)
            {
                var fill = await dailyOccasion.TryFillDailyOccasionAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Daily occasion startup catch-up fill result: {Result}.", fill);
            }

            logger.LogInformation("Daily occasion startup repair completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Daily occasion startup repair failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static DateTimeOffset GetTodayOccasionAt(DateTimeOffset utcNow)
    {
        var date = utcNow.UtcDateTime.Date;
        return new DateTimeOffset(date.Year, date.Month, date.Day, 5, 0, 0, TimeSpan.Zero);
    }
}
