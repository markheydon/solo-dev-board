using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Domain.Entities.Inventory;
using SoloDevBoard.Infrastructure.Inventory;

namespace SoloDevBoard.Functions;

/// <summary>Runs one inventory walk inside a DI scope.</summary>
public sealed class InventoryFunctionsWorker(IServiceScopeFactory scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public async Task RunForTargetAsync(OperatorInventoryTarget target, InventoryRunTrigger trigger, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var credentials = scope.ServiceProvider.GetRequiredService<InventoryOperatorCredentials>();
        credentials.Configure(target);
        var orchestrator = scope.ServiceProvider.GetRequiredService<IInventoryScanOrchestrator>();
        await orchestrator.RunAsync(target.OperatorKey, trigger, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunForOperatorKeyAsync(string operatorKey, InventoryRunTrigger trigger, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var targetProvider = scope.ServiceProvider.GetRequiredService<IOperatorInventoryTargetProvider>();
        var targets = await targetProvider.GetTargetsAsync(cancellationToken).ConfigureAwait(false);
        var target = targets.FirstOrDefault(item => string.Equals(item.OperatorKey, operatorKey, StringComparison.Ordinal));
        if (target is null)
        {
            throw new InvalidOperationException($"No inventory target is configured for operator key '{operatorKey}'.");
        }

        await RunForTargetAsync(target, trigger, cancellationToken).ConfigureAwait(false);
    }
}