using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using SoloDevBoard.Composition;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSoloDevBoard(builder.Configuration);

builder.Build().Run();
