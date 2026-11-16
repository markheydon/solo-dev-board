using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SoloDevBoard.Functions;

/// <summary>Placeholder timer until overnight inventory scanners are implemented.</summary>
public sealed class InventoryPlaceholderTimerFunction(ILogger<InventoryPlaceholderTimerFunction> logger)
{
    [Function(nameof(InventoryPlaceholderTimer))]
    public void InventoryPlaceholderTimer([TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo)
    {
        _ = timerInfo;
        logger.LogInformation("Inventory placeholder timer fired at {TimeUtc}", DateTime.UtcNow);
    }
}
