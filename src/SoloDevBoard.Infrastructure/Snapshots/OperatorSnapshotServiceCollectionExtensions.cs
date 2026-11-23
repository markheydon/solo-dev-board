using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Registers operator snapshot infrastructure services.</summary>
public static class OperatorSnapshotInfrastructureRegistration
{
    /// <summary>Registers file adapter and shared snapshot services.</summary>
    public static IServiceCollection AddOperatorSnapshotInfrastructureServices(this IServiceCollection services)
    {
        services.TryAddScoped<SnapshotCollectionCredentialSlot>();
        services.TryAddSingleton<ISnapshotCollectionCredentialScopeFactory, SnapshotCollectionCredentialScopeFactory>();
        services.TryAddScoped<IOperatorSnapshotStore, FileOperatorSnapshotStore>();
        services.TryAddSingleton<IOperatorSnapshotInProgressReadPolicy, AlwaysRewriteStaleInProgressPolicy>();
        return services;
    }

    /// <summary>Registers the blob adapter as the primary store implementation.</summary>
    public static IServiceCollection AddOperatorSnapshotBlobStore(this IServiceCollection services)
    {
        services.TryAddScoped<IOperatorSnapshotBlobContainer, AzureOperatorSnapshotBlobContainer>();
        services.TryAddSingleton<IOperatorSnapshotInProgressReadPolicy, HostedOperatorSnapshotInProgressReadPolicy>();
        services.AddScoped<IOperatorSnapshotStore, BlobOperatorSnapshotStore>();
        return services;
    }

    /// <summary>Registers schedule blob stores for hosted sign-in.</summary>
    public static IServiceCollection AddOperatorSnapshotScheduleStores(this IServiceCollection services)
    {
        services.TryAddSingleton<ISignInRecordRefreshTokenProtector, SignInRecordRefreshTokenProtector>();
        services.AddScoped<IOperatorSignInRecordStore, OperatorSignInRecordBlobStore>();
        services.AddScoped<IDailyOccasionQueueStore, DailyOccasionQueueBlobStore>();
        return services;
    }
}
