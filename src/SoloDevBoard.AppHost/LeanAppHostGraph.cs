using Aspire.Hosting.Dotnet;

/// <summary>
/// Lean AppHost graph: web compute only (PAT-only local trusted mode and lean publish).
/// </summary>
internal static class LeanAppHostGraph
{
    /// <summary>Configures local run for PAT-only local trusted mode.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigureLocalRun(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var app = host.CreateWebApp();
        app = WireLocalPatOnlyAuth(host, app);
        app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
        host.SetApp(app);
    }

    /// <summary>Adds Container Apps environment and web app for lean publish (no inventory stack).</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigurePublishInfrastructure(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var aca = host.Builder.AddAzureContainerAppEnvironment(host.Naming.AzureName("aca"));
        host.SetContainerAppEnvironment(aca);

        host.SetApp(host.CreateWebApp());
    }

    private static IResourceBuilder<DotnetProjectResource> WireLocalPatOnlyAuth(
        SoloDevBoardAppHost host,
        IResourceBuilder<DotnetProjectResource> app)
    {
        var configuration = host.Builder.Configuration;
        var parameters = host.Parameters;

        var resolvedGithubPat = AppHostDeployParameterResolver.Resolve(configuration, "gh-pat", string.Empty);
        var useLocalPat = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGithubPat);
        var vaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(configuration);
        var needVaultForPat = !useLocalPat && vaultConfigured;

        if (needVaultForPat)
        {
            var authSecretsVault = host.Builder.AddAzureKeyVault("auth-secrets")
                .AsExisting(parameters.AuthSecretsVaultName, parameters.AuthSecretsVaultResourceGroup);

            // Path B local dev: operators grant Key Vault Secrets User manually (see docs/azure-key-vault-setup.md).
            // Skip WithRoleAssignments here to avoid auth-secrets-roles ARM provisioning on every aspire start.
            app = app
                .WithReference(authSecretsVault)
                .WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"));
        }
        else if (useLocalPat)
        {
            app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
        }

        return app;
    }
}
