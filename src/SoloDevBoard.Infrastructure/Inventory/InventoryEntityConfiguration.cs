using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Infrastructure.Inventory;

internal sealed class CanonicalFundingStandardConfiguration : IEntityTypeConfiguration<CanonicalFundingStandard>
{
    public void Configure(EntityTypeBuilder<CanonicalFundingStandard> builder)
    {
        builder.ToTable("Inventory_CanonicalFundingStandard");
        builder.HasKey(static entity => entity.OperatorKey);
        builder.Property(static entity => entity.OperatorKey).HasMaxLength(128);
        builder.Property(static entity => entity.Content).HasColumnType("nvarchar(max)");
        builder.Property(static entity => entity.ContentHash).HasMaxLength(64);
    }
}

internal sealed class InventoryRunConfiguration : IEntityTypeConfiguration<InventoryRun>
{
    public void Configure(EntityTypeBuilder<InventoryRun> builder)
    {
        builder.ToTable("Inventory_InventoryRun");
        builder.HasKey(static entity => entity.Id);
        builder.Property(static entity => entity.OperatorKey).HasMaxLength(128);
        builder.Property(static entity => entity.StandardContentHashAtStart).HasMaxLength(64);
        builder.Property(static entity => entity.FailureReason).HasMaxLength(512);
        builder.HasIndex(static entity => new { entity.OperatorKey, entity.FinishedAtUtc });
        builder.HasIndex(static entity => entity.OperatorKey)
            .HasFilter($"[{nameof(InventoryRun.Outcome)}] IN ({(int)InventoryRunOutcome.Queued}, {(int)InventoryRunOutcome.InProgress})")
            .IsUnique();
        builder.HasMany(static entity => entity.Snapshots)
            .WithOne(static snapshot => snapshot.InventoryRun)
            .HasForeignKey(static snapshot => snapshot.InventoryRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RepositoryInventorySnapshotConfiguration : IEntityTypeConfiguration<RepositoryInventorySnapshot>
{
    public void Configure(EntityTypeBuilder<RepositoryInventorySnapshot> builder)
    {
        builder.ToTable("Inventory_RepositoryInventorySnapshot");
        builder.HasKey(static entity => entity.Id);
        builder.Property(static entity => entity.OperatorKey).HasMaxLength(128);
        builder.Property(static entity => entity.RepositoryFullName).HasMaxLength(256);
        builder.Property(static entity => entity.ComparedPath).HasMaxLength(256);
        builder.Property(static entity => entity.NotedExtraFundingPath).HasMaxLength(256);
        builder.Property(static entity => entity.FundingPlatforms).HasMaxLength(1024);
        builder.Property(static entity => entity.FailureReason).HasMaxLength(512);
        builder.Property(static entity => entity.LicenceName).HasMaxLength(256);
        builder.HasIndex(static entity => new { entity.InventoryRunId, entity.RepositoryFullName }).IsUnique();
        builder.HasIndex(static entity => entity.OperatorKey);
    }
}