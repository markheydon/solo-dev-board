using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Composition;
using SoloDevBoard.Functions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSoloDevBoardHost();
builder.Services.AddSingleton<InventoryFunctionsWorker>();

builder.Build().Run();