using Microsoft.EntityFrameworkCore;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>EF Core context for overnight inventory snapshots.</summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<CanonicalFundingStandard> CanonicalFundingStandards => Set<CanonicalFundingStandard>();

    public DbSet<InventoryRun> InventoryRuns => Set<InventoryRun>();

    public DbSet<RepositoryInventorySnapshot> RepositoryInventorySnapshots => Set<RepositoryInventorySnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}