using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Queues manual inventory scans from the web UI.</summary>
public sealed class InventoryRunCommandService(
    IOperatorKeyResolver operatorKeyResolver,
    IInventoryRunRepository runRepository,
    IInventoryScanQueue scanQueue,
    IOptions<GitHubAuthOptions> authOptions,
    IOptions<InventoryOptions> inventoryOptions) : IInventoryRunCommandService
{
    private readonly IOperatorKeyResolver _operatorKeyResolver = operatorKeyResolver ?? throw new ArgumentNullException(nameof(operatorKeyResolver));
    private readonly IInventoryRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly IInventoryScanQueue _scanQueue = scanQueue ?? throw new ArgumentNullException(nameof(scanQueue));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly InventoryOptions _inventoryOptions = inventoryOptions?.Value ?? throw new ArgumentNullException(nameof(inventoryOptions));

    public async Task<RequestInventoryRunResultDto> RequestScanNowAsync(CancellationToken cancellationToken)
    {
        if (_authOptions.HostedSignInEnabled && !_inventoryOptions.HostedInventoryConfigured
            && (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppId)
                || !AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppPrivateKey)))
        {
            return new RequestInventoryRunResultDto(
                RequestInventoryRunResultKind.Unavailable,
                "Hosted overnight inventory is unavailable.");
        }

        var operatorKey = await _operatorKeyResolver.ResolveCurrentOperatorKeyAsync(cancellationToken).ConfigureAwait(false);
        var activeRun = await _runRepository.GetActiveRunAsync(operatorKey, cancellationToken).ConfigureAwait(false);
        if (activeRun is not null)
        {
            return new RequestInventoryRunResultDto(
                RequestInventoryRunResultKind.AlreadyUnderway,
                "An inventory run is already underway.");
        }

        var run = new InventoryRun
        {
            Id = Guid.NewGuid(),
            OperatorKey = operatorKey,
            Trigger = InventoryRunTrigger.Manual,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Outcome = InventoryRunOutcome.Queued,
            AccessKind = _authOptions.HostedSignInEnabled
                ? InventoryAccessKind.Installation
                : InventoryAccessKind.PersonalAccessToken,
        };
        await _runRepository.AddRunAsync(run, cancellationToken).ConfigureAwait(false);
        await _scanQueue.EnqueueAsync(
            new InventoryScanQueueMessage(operatorKey, DateTimeOffset.UtcNow, null),
            cancellationToken).ConfigureAwait(false);

        return new RequestInventoryRunResultDto(
            RequestInventoryRunResultKind.Accepted,
            "Inventory scan queued.");
    }
}