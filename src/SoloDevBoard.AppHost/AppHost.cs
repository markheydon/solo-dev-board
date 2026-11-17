var builder = DistributedApplication.CreateBuilder(args);
var host = new SoloDevBoardAppHost(builder);
host.AddSharedParameters();

if (builder.ExecutionContext.IsPublishMode)
{
    host.ConfigurePublish();
}
else if (host.UseFullExperiencePosture)
{
    host.ConfigureFullExperienceLocalRun();
}
else
{
    host.ConfigureLeanLocalRun();
}

builder.Build().Run();
