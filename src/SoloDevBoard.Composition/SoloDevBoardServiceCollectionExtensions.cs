using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Infrastructure.Common;
using SoloDevBoard.Infrastructure.Inventory;

namespace SoloDevBoard.Composition;

/// <summary>Extension methods for registering SoloDevBoard services with the DI container.</summary>
public static class SoloDevBoardServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application and Infrastructure services. Call this from each host composition root
    /// (for example Blazor Server or a future Functions worker).
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">Application configuration used for options binding.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSoloDevBoard(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);

        return services;
    }

    /// <summary>Registers Aspire SQL and Solo Dev Board services for web and worker hosts.</summary>
    public static void AddSoloDevBoardHost(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddSqlServerDbContext<InventoryDbContext>(
            "inventory",
            configureSettings: null,
            configureDbContextOptions: static options =>
                options.UseSqlServer(static sql => sql.MigrationsAssembly("SoloDevBoard.Inventory.Migrations")));
        builder.Services.AddSoloDevBoard(builder.Configuration);
    }

    /// <summary>Registers the inventory scan queue client for the Web host only (constitution VI).</summary>
    public static void AddSoloDevBoardWebInventoryQueues(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAzureQueueServiceClient("inventory-queues");
        builder.Services.AddScoped<IInventoryScanQueue, AzureInventoryScanQueue>();
    }
}
