using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.AddScoped<OperatorSnapshotEligibility>();
        services.AddScoped<OperatorSnapshotScheduleStatusBuilder>();
        services.AddScoped<IHostedSignInRecordService, HostedSignInRecordService>();
        services.AddScoped<IOperatorSnapshotCollectionService, OperatorSnapshotCollectionService>();
        return services;
    }

    /// <summary>Registers hosted schedule stores and removes lean stubs.</summary>
    public static IServiceCollection AddOperatorSnapshotScheduleServices(this IServiceCollection services)
    {
        services.AddOperatorSnapshotScheduleStores();
        return services;
    }

    /// <summary>Registers no-op schedule stores for lean file posture.</summary>
    public static IServiceCollection AddOperatorSnapshotScheduleLeanStubs(this IServiceCollection services)
    {
        services.TryAddScoped<IOperatorSignInRecordStore, NoOpOperatorSignInRecordStore>();
        services.TryAddScoped<IDailyOccasionQueueStore, NoOpDailyOccasionQueueStore>();
        services.TryAddSingleton<ISignInRecordRefreshTokenProtector, NoOpSignInRecordRefreshTokenProtector>();
        return services;
    }
}
