using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.Identity;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Composition;

/// <summary>Registers hosted scheduled-collection adapters from Aspire-injected storage clients.</summary>
public static class ScheduledCollectionHostExtensions
{
    /// <summary>
    /// Registers sign-in blob adapters after <c>AddKeyedAzureBlobContainerClient</c> for
    /// <see cref="AspireHostedStorageReferenceNames.HostedSignIns" />.
    /// </summary>
    public static IServiceCollection RegisterHostedSignInBlobStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IHostedSignInRecordStore>();
        services.AddSingleton(sp => new HostedSignInsBlobContainer(
            sp.GetRequiredKeyedService<BlobContainerClient>(AspireHostedStorageReferenceNames.HostedSignIns)));
        services.AddSingleton<IHostedSignInRecordStore, BlobHostedSignInRecordStore>();
        return services;
    }

    /// <summary>
    /// Registers worker schedule and queue adapters after keyed blob clients and
    /// <c>AddAzureQueueServiceClient</c> for <see cref="AspireHostedStorageReferenceNames.OperatorSnapshotQueues" />.
    /// </summary>
    public static IServiceCollection RegisterHostedScheduledCollectionWorkerAdapters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddHostedScheduledCollectionWorkerAdapters();
    }
}
