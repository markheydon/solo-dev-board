using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Registers scheduled snapshot collection adapters.</summary>
public static class ScheduledCollectionInfrastructureRegistration
{
    /// <summary>Registers null schedule stores for lean posture.</summary>
    public static IServiceCollection AddLeanScheduledCollection(this IServiceCollection services)
    {
        services.TryAddSingleton<IHostedSignInRecordStore, NullHostedSignInRecordStore>();
        services.TryAddSingleton<ICollectionOccasionMarkerStore, NullCollectionOccasionMarkerStore>();
        services.TryAddSingleton<IDailySnapshotQueuePublisher, NullDailySnapshotQueuePublisher>();
        services.TryAddSingleton<IDailyCollectionRunnerLease, NullDailyCollectionRunnerLease>();
        return services;
    }

    /// <summary>Registers blob and queue adapters for hosted posture.</summary>
    public static IServiceCollection AddHostedScheduledCollection(
        this IServiceCollection services,
        BlobContainerClient hostedSignInsContainer,
        BlobContainerClient collectionScheduleContainer,
        QueueServiceClient queueServiceClient)
    {
        ArgumentNullException.ThrowIfNull(hostedSignInsContainer);
        ArgumentNullException.ThrowIfNull(collectionScheduleContainer);
        ArgumentNullException.ThrowIfNull(queueServiceClient);

        services.AddSingleton(new HostedSignInsBlobContainer(hostedSignInsContainer));
        services.AddSingleton(new CollectionScheduleBlobContainer(collectionScheduleContainer));
        services.AddSingleton(queueServiceClient);
        services.TryAddSingleton<IHostedSignInRecordStore, BlobHostedSignInRecordStore>();
        services.TryAddSingleton<ICollectionOccasionMarkerStore, BlobCollectionOccasionMarkerStore>();
        services.TryAddSingleton<IDailySnapshotQueuePublisher, AzureDailySnapshotQueuePublisher>();
        services.TryAddSingleton<IDailyCollectionRunnerLease, BlobDailyCollectionRunnerLease>();
        return services;
    }
}
