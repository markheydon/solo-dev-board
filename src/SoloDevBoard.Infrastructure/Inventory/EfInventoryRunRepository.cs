using Microsoft.EntityFrameworkCore;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>EF Core inventory run repository.</summary>
public sealed class EfInventoryRunRepository(InventoryDbContext dbContext) : IInventoryRunRepository
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<InventoryRun?> GetActiveRunAsync(string operatorKey, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryRuns
            .AsNoTracking()
            .Where(run => run.OperatorKey == operatorKey)
            .Where(run => run.Outcome == InventoryRunOutcome.Queued || run.Outcome == InventoryRunOutcome.InProgress)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<InventoryRun?> GetLatestFinishedRunAsync(string operatorKey, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryRuns
            .AsNoTracking()
            .Where(run => run.OperatorKey == operatorKey)
            .Where(run => !run.IsSuperseded)
            .Where(run => run.Outcome == InventoryRunOutcome.Completed || run.Outcome == InventoryRunOutcome.Partial)
            .Where(run => run.FinishedAtUtc != null)
            .OrderByDescending(run => run.FinishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<InventoryRun?> GetLatestAttemptAsync(string operatorKey, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryRuns
            .AsNoTracking()
            .Where(run => run.OperatorKey == operatorKey)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RepositoryInventorySnapshot>> GetSnapshotsForRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        return await _dbContext.RepositoryInventorySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.InventoryRunId == runId)
            .OrderBy(snapshot => snapshot.RepositoryFullName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddRunAsync(InventoryRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        _dbContext.InventoryRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateRunAsync(InventoryRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        _dbContext.InventoryRuns.Update(run);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddSnapshotsAsync(IEnumerable<RepositoryInventorySnapshot> snapshots, CancellationToken cancellationToken)
    {
        _dbContext.RepositoryInventorySnapshots.AddRange(snapshots);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SupersedeFinishedRunsAsync(string operatorKey, Guid exceptRunId, CancellationToken cancellationToken)
    {
        await _dbContext.InventoryRuns
            .Where(run => run.OperatorKey == operatorKey && run.Id != exceptRunId)
            .Where(run => run.Outcome == InventoryRunOutcome.Completed || run.Outcome == InventoryRunOutcome.Partial)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(run => run.IsSuperseded, true),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InventoryRun>> GetStuckRunsAsync(TimeSpan stuckThreshold, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow - stuckThreshold;
        return await _dbContext.InventoryRuns
            .Where(run => run.Outcome == InventoryRunOutcome.Queued || run.Outcome == InventoryRunOutcome.InProgress)
            .Where(run => run.StartedAtUtc < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}