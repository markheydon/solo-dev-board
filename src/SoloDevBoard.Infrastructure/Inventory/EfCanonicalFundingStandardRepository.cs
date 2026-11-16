using Microsoft.EntityFrameworkCore;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>EF Core canonical funding standard repository.</summary>
public sealed class EfCanonicalFundingStandardRepository(InventoryDbContext dbContext) : ICanonicalFundingStandardRepository
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<CanonicalFundingStandard?> GetByOperatorKeyAsync(string operatorKey, CancellationToken cancellationToken)
    {
        return await _dbContext.CanonicalFundingStandards
            .AsNoTracking()
            .FirstOrDefaultAsync(standard => standard.OperatorKey == operatorKey, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(CanonicalFundingStandard standard, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(standard);
        var existing = await _dbContext.CanonicalFundingStandards
            .FirstOrDefaultAsync(entity => entity.OperatorKey == standard.OperatorKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.CanonicalFundingStandards.Add(standard);
        }
        else
        {
            existing.Content = standard.Content;
            existing.SavedAtUtc = standard.SavedAtUtc;
            existing.ContentHash = standard.ContentHash;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}