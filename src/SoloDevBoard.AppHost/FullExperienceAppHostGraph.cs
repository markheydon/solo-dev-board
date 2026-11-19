using Aspire.Hosting.Azure;
using Aspire.Hosting.Dotnet;

/// <summary>
/// Full experience AppHost graph: inventory SQL, Functions, storage, and web compute.
/// </summary>
internal static class FullExperienceAppHostGraph
{
    /// <summary>Configures local run with the full inventory stack and optional dev Key Vault.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigureLocalRun(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        AddInventoryStack(host, out var app, out var functions);
        app = WireLocalHostedSignInAuth(host, app, functions, out functions);
        app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
        host.SetApp(app);
        host.SetFunctions(functions);
    }

    /// <summary>Adds Azure deploy infrastructure for the full experience graph.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigurePublishInfrastructure(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        AddInventoryStack(host, out var app, out var functions);
        host.SetApp(app);
        host.SetFunctions(functions);
    }

    private static void AddInventoryStack(
        SoloDevBoardAppHost host,
        out IResourceBuilder<DotnetProjectResource> app,
        out IResourceBuilder<AzureFunctionsProjectResource> functions)
    {
        var aca = host.Builder.AddAzureContainerAppEnvironment(host.Naming.AzureName("aca"));
        host.SetContainerAppEnvironment(aca);

        var sql = host.Builder.AddAzureSqlServer(host.Naming.AzureName("sql"))
            .RunAsContainer(container => container.WithDataVolume("inventory-sql-data"));

        var inventory = sql.AddDatabase("inventory");

        var storage = host.Builder.AddAzureStorage(host.Naming.AzureStorageResourceName())
            .RunAsEmulator();

        functions = host.Builder.AddAzureFunctionsProject(
                host.Naming.AzureName("functions"),
                "../SoloDevBoard.Functions/SoloDevBoard.Functions.csproj")
            .WithHostStorage(storage)
            .WithReference(inventory)
            .WaitFor(inventory);

        app = host.CreateWebApp().WithReference(inventory);
    }

    private static IResourceBuilder<DotnetProjectResource> WireLocalHostedSignInAuth(
        SoloDevBoardAppHost host,
        IResourceBuilder<DotnetProjectResource> app,
        IResourceBuilder<AzureFunctionsProjectResource> functions,
        out IResourceBuilder<AzureFunctionsProjectResource> wiredFunctions)
    {
        var configuration = host.Builder.Configuration;
        var parameters = host.Parameters;

        var hostedSignIn = AppHostDeployParameterResolver.IsHostedSignInEnabled(configuration);
        var resolvedGithubPat = AppHostDeployParameterResolver.Resolve(configuration, "gh-pat", string.Empty);
        var resolvedGhAppClientSecret = AppHostDeployParameterResolver.Resolve(configuration, "gh-app-client-secret", string.Empty);
        var useLocalPat = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGithubPat);
        var useLocalClientSecret = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGhAppClientSecret);
        var vaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(configuration);

        var needVaultForPat = !hostedSignIn && !useLocalPat && vaultConfigured;
        var needVaultForClientSecret = hostedSignIn && !useLocalClientSecret && vaultConfigured;

        wiredFunctions = functions;

        if (needVaultForPat || needVaultForClientSecret)
        {
            var authSecretsVault = host.Builder.AddAzureKeyVault("auth-secrets")
                .AsExisting(parameters.AuthSecretsVaultName, parameters.AuthSecretsVaultResourceGroup);

            // Path B local dev: operators grant Key Vault Secrets User manually (see docs/azure-key-vault-setup.md).
            // Skip WithRoleAssignments here to avoid auth-secrets-roles ARM provisioning on every aspire start.
            app = app.WithReference(authSecretsVault);
            wiredFunctions = wiredFunctions
                .WithReference(authSecretsVault)
                .WithEnvironment("GitHubAuth__HostedSignInEnabled", "false");

            if (needVaultForPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"));
                wiredFunctions = wiredFunctions.WithEnvironment(
                    "GitHubAuth__PersonalAccessToken",
                    authSecretsVault.GetSecret("gh-pat"));
            }
            else if (useLocalPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
                wiredFunctions = wiredFunctions.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
            }

            if (needVaultForClientSecret)
            {
                app = app.WithEnvironment(
                    "GitHubAuth__HostedGitHubAppClientSecret",
                    authSecretsVault.GetSecret("gh-app-client-secret"));
            }
            else if (useLocalClientSecret)
            {
                app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", parameters.GhAppClientSecret);
            }
        }
        else
        {
            wiredFunctions = wiredFunctions.WithEnvironment("GitHubAuth__HostedSignInEnabled", "false");

            if (useLocalPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
                wiredFunctions = wiredFunctions.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
            }

            if (useLocalClientSecret)
            {
                app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", parameters.GhAppClientSecret);
            }
        }

        return app;
    }
}
