using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Composition;

/// <summary>Web host registration for operator snapshot collection.</summary>
public static class OperatorSnapshotWebHostExtensions
{
    /// <summary>Configures file or blob storage, durability options, and collection services.</summary>
    /// <param name="builder">Web application builder.</param>
    /// <param name="useBlobStore">When true, registers the blob store (call after <c>AddAzureBlobContainerClient</c> in the web host).</param>
    public static WebApplicationBuilder AddOperatorSnapshotHost(this WebApplicationBuilder builder, bool useBlobStore)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var snapshotDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "operator-snapshots");
        builder.Services.Configure<OperatorSnapshotFileStoreOptions>(options => options.SnapshotDirectory = snapshotDirectory);

        if (useBlobStore)
        {
            builder.Services.AddOperatorSnapshotBlobStore();
            builder.Services.AddOperatorSnapshotScheduleServices();
        }
        else
        {
            builder.Services.AddOperatorSnapshotScheduleLeanStubs();
        }

        var durabilityKind = useBlobStore
            ? OperatorSnapshotDurabilityKind.HostedBlob
            : !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTAINER_APP_NAME"))
                ? OperatorSnapshotDurabilityKind.EphemeralContainerDisk
                : OperatorSnapshotDurabilityKind.LocalMachine;

        builder.Services.Configure<OperatorSnapshotDurabilityOptions>(options => options.DurabilityKind = durabilityKind);
        OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotServices(builder.Services);
        return builder;
    }
}
