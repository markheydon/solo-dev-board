using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SoloDevBoard.Infrastructure.Inventory;

namespace SoloDevBoard.Inventory.Migrations;

/// <summary>Design-time factory for EF CLI migrations against the inventory context.</summary>
public sealed class InventoryDesignTimeDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=inventory;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(InventoryDesignTimeDbContextFactory).Assembly.GetName().Name))
            .Options;
        return new InventoryDbContext(options);
    }
}