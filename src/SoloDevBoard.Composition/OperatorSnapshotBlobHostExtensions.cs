using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Composition;

/// <summary>Registers operator snapshot blob adapters from Aspire-injected clients.</summary>
public static class OperatorSnapshotBlobHostExtensions
{
    /// <summary>
    /// Registers the blob store using a keyed <see cref="BlobContainerClient"/> from
    /// <c>AddKeyedAzureBlobContainerClient</c>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="containerConnectionName">AppHost reference name and DI key.</param>
    public static IServiceCollection AddOperatorSnapshotKeyedBlobStore(
        this IServiceCollection services,
        string containerConnectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerConnectionName);

        services.AddScoped<IOperatorSnapshotBlobContainer>(sp =>
            new AzureOperatorSnapshotBlobContainer(
                sp.GetRequiredKeyedService<BlobContainerClient>(containerConnectionName)));
        services.AddScoped<IOperatorSnapshotStore, BlobOperatorSnapshotStore>();
        return services;
    }
}
