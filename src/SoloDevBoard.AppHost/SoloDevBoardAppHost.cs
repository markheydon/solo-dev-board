using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Dotnet;

/// <summary>
/// Orchestrates SoloDevBoard AppHost graph selection and shared state.
/// </summary>
internal sealed class SoloDevBoardAppHost
{
    /// <summary>Creates an orchestrator for the AppHost entry point.</summary>
    /// <param name="builder">Distributed application builder.</param>
    public SoloDevBoardAppHost(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Builder = builder;
        Naming = new AppHostAzureNaming(builder.Environment.EnvironmentName);
    }

    /// <summary>Aspire distributed application builder.</summary>
    public IDistributedApplicationBuilder Builder { get; }

    /// <summary>Tier-aware Azure resource naming.</summary>
    public AppHostAzureNaming Naming { get; }

    /// <summary>Shared auth and vault parameters.</summary>
    public AppHostParameters Parameters { get; private set; } = null!;

    /// <summary>Blazor web compute resource.</summary>
    public IResourceBuilder<DotnetProjectResource> App { get; private set; } = null!;

    /// <summary>Azure Container Apps environment when publishing.</summary>
    public IResourceBuilder<AzureContainerAppEnvironmentResource>? ContainerAppEnvironment { get; private set; }

    /// <summary>
    /// Returns <see langword="true" /> when hosted sign-in is enabled (hosted graph: web compute only).
    /// </summary>
    public bool UseHostedSignInPosture =>
        AppHostPostureResolver.UseHostedSignInPosture(Builder.Configuration);

    /// <summary>Declares parameters used by every graph.</summary>
    public void AddSharedParameters() => Parameters = AppHostParameters.AddShared(Builder);

    /// <summary>Models PAT-only local trusted mode: web compute and optional dev Key Vault.</summary>
    public void ConfigureLeanLocalRun() => LeanAppHostGraph.ConfigureLocalRun(this);

    /// <summary>Models hosted sign-in locally (web compute; optional dev Key Vault).</summary>
    public void ConfigureHostedSignInLocalRun() => HostedSignInAppHostGraph.ConfigureLocalRun(this);

    /// <summary>Models Azure deploy for lean or hosted sign-in posture.</summary>
    public void ConfigurePublish() => AppHostPublishProfile.Configure(this);

    /// <summary>Creates the web project resource with shared auth environment bindings.</summary>
    internal IResourceBuilder<DotnetProjectResource> CreateWebApp()
    {
        var app = Builder.AddDotnetProject(Naming.AzureName("web"), "../SoloDevBoard.Web/SoloDevBoard.Web.csproj");

        return app
            .WithHttpHealthCheck("/health")
            .WithExternalHttpEndpoints()
            .WithEnvironment("GitHubAuth__HostedSignInEnabled", Parameters.HostedSignInEnabled)
            .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", Parameters.GhAppClientId)
            .WithEnvironment("HostedAdmissionControl__Enabled", Parameters.HostedAdmissionEnabled)
            .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", Parameters.AllowedUserLogins)
            .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", Parameters.AllowedOrgLogins)
            .PublishAsAzureContainerApp((_, containerApp) =>
            {
                containerApp.Template.Scale.MinReplicas = 0;
                containerApp.Template.Scale.MaxReplicas = 1;
            });
    }

    /// <summary>Stores the web app builder.</summary>
    /// <param name="app">Web app resource builder.</param>
    internal void SetApp(IResourceBuilder<DotnetProjectResource> app) => App = app;

    /// <summary>Stores the Container Apps environment builder.</summary>
    /// <param name="aca">Container App environment resource builder.</param>
    internal void SetContainerAppEnvironment(IResourceBuilder<AzureContainerAppEnvironmentResource> aca) =>
        ContainerAppEnvironment = aca;
}
