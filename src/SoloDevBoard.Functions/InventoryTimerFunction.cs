using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Functions;

/// <summary>Nightly open-source inventory timer.</summary>
public sealed class InventoryTimerFunction(InventoryFunctionsWorker worker, IServiceScopeFactory scopeFactory)
{
    private readonly InventoryFunctionsWorker _worker = worker ?? throw new ArgumentNullException(nameof(worker));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    [Function(nameof(InventoryTimerFunction))]
    public async Task Run([TimerTrigger("%Inventory__ScheduledCron%")] TimerInfo timer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var targetProvider = scope.ServiceProvider.GetRequiredService<IOperatorInventoryTargetProvider>();
        var targets = await targetProvider.GetTargetsAsync(cancellationToken).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var target in targets)
        {
            await _worker.RunForTargetAsync(target, InventoryRunTrigger.Scheduled, cancellationToken).ConfigureAwait(false);
        }
    }
}