using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.Identity;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Composition;

/// <summary>Registers hosted scheduled collection storage adapters from connection strings.</summary>
public static class ScheduledCollectionHostExtensions
{
    /// <summary>Registers the sign-in blob store when <c>hosted-sign-ins</c> is configured.</summary>
    public static void AddHostedSignInRecordBlobIfConfigured(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var connectionString = builder.Configuration.GetConnectionString("hosted-sign-ins");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        builder.Services.RemoveAll<IHostedSignInRecordStore>();
        builder.Services.AddSingleton(new HostedSignInsBlobContainer(new BlobContainerClient(connectionString, "hosted-sign-ins")));
        builder.Services.AddSingleton<IHostedSignInRecordStore, BlobHostedSignInRecordStore>();
    }

    /// <summary>Registers queue and schedule adapters for the Functions worker.</summary>
    public static void AddHostedScheduledCollectionWorkerIfConfigured(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var configuration = builder.Configuration;
        var signIns = configuration.GetConnectionString("hosted-sign-ins");
        var schedule = configuration.GetConnectionString("collection-schedule");
        var queue = configuration.GetConnectionString("operator-snapshot-queues");
        if (string.IsNullOrWhiteSpace(signIns)
            || string.IsNullOrWhiteSpace(schedule)
            || string.IsNullOrWhiteSpace(queue))
        {
            return;
        }

        var hostedClient = new BlobContainerClient(signIns, "hosted-sign-ins");
        var scheduleClient = new BlobContainerClient(schedule, "collection-schedule");
        var queueClient = new QueueServiceClient(queue);
        builder.Services.AddHostedScheduledCollection(hostedClient, scheduleClient, queueClient);
    }
}
