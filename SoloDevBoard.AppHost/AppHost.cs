var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SoloDevBoard_App>("app")
	.WithExternalHttpEndpoints();

builder.Build().Run();
