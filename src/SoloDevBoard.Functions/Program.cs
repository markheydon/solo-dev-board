using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Composition;
using SoloDevBoard.Infrastructure.Planning;
var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var configuration = builder.Configuration;
var useHostedSnapshots = !string.IsNullOrWhiteSpace(configuration.GetConnectionString(AspireHostedStorageReferenceNames.OperatorSnapshots));
var useHostedWorker = useHostedSnapshots
    && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(AspireHostedStorageReferenceNames.HostedSignIns))
    && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(AspireHostedStorageReferenceNames.CollectionSchedule))
    && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(AspireHostedStorageReferenceNames.OperatorSnapshotQueues));

if (useHostedSnapshots)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.OperatorSnapshots);
    builder.Services.AddOperatorSnapshotKeyedBlobStore(AspireHostedStorageReferenceNames.OperatorSnapshots);
    builder.Services.Configure<OperatorSnapshotDurabilityOptions>(static options =>
        options.DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob);
}

if (useHostedWorker)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.HostedSignIns);
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.CollectionSchedule);
    builder.AddAzureQueueServiceClient(AspireHostedStorageReferenceNames.OperatorSnapshotQueues);
    builder.Services.RegisterHostedScheduledCollectionWorkerAdapters();
}

builder.Services.TryAddSingleton<IPlanningSettingsStorage, WorkerPlanningSettingsStorage>();
builder.Services.AddSoloDevBoard(builder.Configuration);
OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotServices(builder.Services);
OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotScheduledCollectionWorkerServices(builder.Services);

builder.Build().Run();
