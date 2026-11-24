using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Registers scheduled snapshot collection adapters.</summary>
public static class ScheduledCollectionInfrastructureRegistration
{
    /// <summary>AppHost reference name for sign-in blobs (must match keyed client registration).</summary>
    public const string HostedSignInsConnectionName = "hosted-sign-ins";

    /// <summary>AppHost reference name for schedule blobs (must match keyed client registration).</summary>
    public const string CollectionScheduleConnectionName = "collection-schedule";

    /// <summary>Registers null schedule stores for lean posture.</summary>
    public static IServiceCollection AddLeanScheduledCollection(this IServiceCollection services)
    {
        services.TryAddSingleton<IHostedSignInRecordStore, NullHostedSignInRecordStore>();
        services.TryAddSingleton<ICollectionOccasionMarkerStore, NullCollectionOccasionMarkerStore>();
        services.TryAddSingleton<IDailySnapshotQueuePublisher, NullDailySnapshotQueuePublisher>();
        services.TryAddSingleton<IDailyCollectionRunnerLease, NullDailyCollectionRunnerLease>();
        return services;
    }

    /// <summary>
    /// Registers blob and queue adapters for the Functions worker using keyed Aspire clients and
    /// the default <see cref="Azure.Storage.Queues.QueueServiceClient" /> from
    /// <c>AddAzureQueueServiceClient</c>.
    /// </summary>
    public static IServiceCollection AddHostedScheduledCollectionWorkerAdapters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp => new HostedSignInsBlobContainer(
            sp.GetRequiredKeyedService<BlobContainerClient>(HostedSignInsConnectionName)));
        services.AddSingleton(sp => new CollectionScheduleBlobContainer(
            sp.GetRequiredKeyedService<BlobContainerClient>(CollectionScheduleConnectionName)));
        services.TryAddSingleton<IHostedSignInRecordStore, BlobHostedSignInRecordStore>();
        services.TryAddSingleton<ICollectionOccasionMarkerStore, BlobCollectionOccasionMarkerStore>();
        services.TryAddSingleton<IDailySnapshotQueuePublisher, AzureDailySnapshotQueuePublisher>();
        services.TryAddSingleton<IDailyCollectionRunnerLease, BlobDailyCollectionRunnerLease>();
        return services;
    }
}
