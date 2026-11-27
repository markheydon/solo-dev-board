using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Composition;
using SoloDevBoard.Functions;
using SoloDevBoard.Infrastructure.Planning;
var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var configuration = builder.Configuration;
var hostedSignInEnabled = configuration.GetValue<bool>(
    $"{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.HostedSignInEnabled)}");
var useOperatorSnapshotBlob = !string.IsNullOrWhiteSpace(configuration["ConnectionStrings:operator-snapshots"]);
var useHostedWorker = hostedSignInEnabled && useOperatorSnapshotBlob;

if (useOperatorSnapshotBlob)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.OperatorSnapshots);
    builder.Services.AddOperatorSnapshotKeyedBlobStore(AspireHostedStorageReferenceNames.OperatorSnapshots);
    builder.Services.Configure<OperatorSnapshotDurabilityOptions>(static options =>
        options.DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob);
}

if (useHostedWorker)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.HostedSignIns);
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.AccountScanStatus);
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.CollectionSchedule);
    builder.AddAzureQueueServiceClient(AspireHostedStorageReferenceNames.OperatorSnapshotQueues);
    builder.Services.RegisterHostedScheduledCollectionWorkerAdapters();
    builder.Services.AddHostedService<DailyOccasionStartupRepairHostedService>();
}

builder.Services.TryAddSingleton<IPlanningSettingsStorage, WorkerPlanningSettingsStorage>();
builder.Services.AddSoloDevBoard(builder.Configuration);
OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotServices(builder.Services);
OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotScheduledCollectionWorkerServices(builder.Services);

builder.Build().Run();
