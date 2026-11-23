using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Composition;

/// <summary>Registers operator snapshot application services.</summary>
public static class OperatorSnapshotServiceCollectionExtensions
{
    /// <summary>Registers the collection coordinator and scoped collection service.</summary>
    /// <param name="services">Service collection.</param>
    public static IServiceCollection AddOperatorSnapshotServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOperatorSnapshotInfrastructureServices();
        services.AddSingleton<OperatorSnapshotApplicationStopping>(static sp =>
            new OperatorSnapshotApplicationStopping(sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping));
        services.AddSingleton<OperatorSnapshotCollectionCoordinator>();
        services.AddSingleton<IOperatorSnapshotLiveTaskRegistry>(static sp =>
            sp.GetRequiredService<OperatorSnapshotCollectionCoordinator>());
        services.AddScoped<IOperatorSnapshotCollectionService, OperatorSnapshotCollectionService>();
        return services;
    }
}
