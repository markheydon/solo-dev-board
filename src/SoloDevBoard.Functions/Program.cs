using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Composition;
using SoloDevBoard.Infrastructure.Snapshots;
var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var useHosted = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("operator-snapshots"));
if (useHosted)
{
    builder.AddAzureBlobContainerClient("operator-snapshots");
    builder.Services.AddOperatorSnapshotBlobStore();
    builder.Services.Configure<OperatorSnapshotDurabilityOptions>(static options =>
        options.DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob);
}

builder.AddHostedScheduledCollectionWorkerIfConfigured();
builder.Services.AddSoloDevBoard(builder.Configuration);
OperatorSnapshotServiceCollectionExtensions.AddOperatorSnapshotServices(builder.Services);

builder.Build().Run();
