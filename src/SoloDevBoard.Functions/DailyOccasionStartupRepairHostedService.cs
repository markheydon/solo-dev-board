using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Functions;

/// <summary>Runs missing <c>initial</c> queue repair once when the worker starts.</summary>
internal sealed class DailyOccasionStartupRepairHostedService(
    IDailyOccasionService dailyOccasion,
    ILogger<DailyOccasionStartupRepairHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dailyOccasion.RepairMissingInitialMessagesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Daily occasion startup repair completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Daily occasion startup repair failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
