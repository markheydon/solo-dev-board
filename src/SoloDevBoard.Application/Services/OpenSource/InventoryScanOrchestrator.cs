using Microsoft.Extensions.Logging;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Coordinates scheduled and manual inventory walks.</summary>
public sealed class InventoryScanOrchestrator(
    IInventoryRunRepository runRepository,
    ICanonicalFundingStandardRepository standardRepository,
    IGitHubOpenSourceInventoryScanner scanner,
    IInventoryCatalogueClient catalogueClient,
  IInventoryOperatorCredentials credentials,
    ILogger<InventoryScanOrchestrator> logger) : IInventoryScanOrchestrator
{
    private static readonly TimeSpan StuckRunGrace = TimeSpan.FromMinutes(5);

    private readonly IInventoryRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly ICanonicalFundingStandardRepository _standardRepository = standardRepository ?? throw new ArgumentNullException(nameof(standardRepository));
    private readonly IGitHubOpenSourceInventoryScanner _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    private readonly IInventoryCatalogueClient _catalogueClient = catalogueClient ?? throw new ArgumentNullException(nameof(catalogueClient));
    private readonly IInventoryOperatorCredentials _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    private readonly ILogger<InventoryScanOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task RunAsync(string operatorKey, InventoryRunTrigger trigger, CancellationToken cancellationToken)
    {
        await RecoverStuckRunsAsync(cancellationToken).ConfigureAwait(false);

        if (!string.Equals(operatorKey, _credentials.OperatorKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Operator key does not match the active inventory credentials scope.");
        }

        var activeRun = await _runRepository.GetActiveRunAsync(operatorKey, cancellationToken).ConfigureAwait(false);
        InventoryRun run;
        if (activeRun is { Outcome: InventoryRunOutcome.Queued })
        {
            run = activeRun;
            run.Outcome = InventoryRunOutcome.InProgress;
            run.StartedAtUtc = DateTimeOffset.UtcNow;
            await _runRepository.UpdateRunAsync(run, cancellationToken).ConfigureAwait(false);
        }
        else if (activeRun is { Outcome: InventoryRunOutcome.InProgress })
        {
            _logger.LogInformation("Inventory for {OperatorKey} already in progress; skipping duplicate walk.", operatorKey);
            return;
        }
        else
        {
            run = await CreateRunAsync(operatorKey, trigger, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var standard = await _standardRepository.GetByOperatorKeyAsync(operatorKey, cancellationToken).ConfigureAwait(false);
            var standardContent = standard?.Content;
            run.StandardContentHashAtStart = standard?.ContentHash ?? string.Empty;
            run.AccessKind = _credentials.UsesInstallationToken
                ? InventoryAccessKind.Installation
                : InventoryAccessKind.PersonalAccessToken;

            var repositories = await _catalogueClient.GetOpenSourceRepositoriesAsync(cancellationToken).ConfigureAwait(false);
            run.IntendedRepositoryCount = repositories.Count;
            var snapshots = new List<RepositoryInventorySnapshot>();
            var deadline = DateTimeOffset.UtcNow.AddMinutes(9);

            foreach (var repository in repositories)
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    run.Outcome = InventoryRunOutcome.Partial;
                    break;
                }

                try
                {
                    var snapshot = await _scanner.ScanRepositoryAsync(
                        run,
                        repository.FullName,
                        repository.IsArchived,
                        repository.IsFork,
                        repository.IsPrivate,
                        repository.Description,
                        standardContent,
                        cancellationToken).ConfigureAwait(false);
                    snapshots.Add(snapshot);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    run.WasRateLimited = true;
                    _logger.LogWarning(ex, "Rate limited while scanning {Repository}.", repository.FullName);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan {Repository}.", repository.FullName);
                    snapshots.Add(new RepositoryInventorySnapshot
                    {
                        InventoryRunId = run.Id,
                        OperatorKey = operatorKey,
                        RepositoryFullName = repository.FullName,
                        IsArchived = repository.IsArchived,
                        IsFork = repository.IsFork,
                        IsPrivate = repository.IsPrivate,
                        ScanState = RepositoryScanState.NotScanned,
                        FailureReason = "Could not read repository.",
                    });
                }
            }

            if (run.Outcome != InventoryRunOutcome.Partial)
            {
                run.Outcome = snapshots.Count >= run.IntendedRepositoryCount
                    ? InventoryRunOutcome.Completed
                    : InventoryRunOutcome.Partial;
            }

            run.CheckedRepositoryCount = snapshots.Count(static item => item.ScanState == RepositoryScanState.Checked);
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            await _runRepository.UpdateRunAsync(run, cancellationToken).ConfigureAwait(false);
            await _runRepository.AddSnapshotsAsync(snapshots, cancellationToken).ConfigureAwait(false);
            await _runRepository.SupersedeFinishedRunsAsync(operatorKey, run.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            run.Outcome = InventoryRunOutcome.Failed;
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.FailureReason = ex.Message;
            await _runRepository.UpdateRunAsync(run, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task RecoverStuckRunsAsync(CancellationToken cancellationToken)
    {
        var stuck = await _runRepository.GetStuckRunsAsync(StuckRunGrace + TimeSpan.FromMinutes(30), cancellationToken).ConfigureAwait(false);
        foreach (var run in stuck)
        {
            run.Outcome = InventoryRunOutcome.Failed;
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.FailureReason = "Run timed out.";
            await _runRepository.UpdateRunAsync(run, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<InventoryRun> CreateRunAsync(string operatorKey, InventoryRunTrigger trigger, CancellationToken cancellationToken)
    {
        var run = new InventoryRun
        {
            Id = Guid.NewGuid(),
            OperatorKey = operatorKey,
            Trigger = trigger,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Outcome = InventoryRunOutcome.InProgress,
        };
        await _runRepository.AddRunAsync(run, cancellationToken).ConfigureAwait(false);
        return run;
    }
}