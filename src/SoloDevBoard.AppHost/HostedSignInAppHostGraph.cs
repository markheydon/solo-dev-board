using Aspire.Hosting.Azure;
using Aspire.Hosting.Dotnet;
using Azure.Provisioning.Storage;

/// <summary>
/// Hosted sign-in AppHost graph: web compute with GitHub App OAuth and admission control.
/// Operator snapshot blob storage is modelled (ADR-0048); scheduled collection uses ADR-0049 Functions worker.
/// </summary>
internal static class HostedSignInAppHostGraph
{
    /// <summary>Configures local run for hosted sign-in with optional dev Key Vault.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigureLocalRun(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var storage = host.Builder.AddAzureStorage("operator-snapshot-storage")
            .RunAsEmulator(azurite => azurite.WithDataVolume());
        var snapshots = storage.AddBlobContainer("operator-snapshots", "operator-snapshots");
        var hostedSignIns = storage.AddBlobContainer("hosted-sign-ins", "hosted-sign-ins");
        var collectionSchedule = storage.AddBlobContainer("collection-schedule", "collection-schedule");
        var queues = storage.AddQueues("operator-snapshot-queues");

        var functions = host.Builder
            .AddAzureFunctionsProject(
                host.Naming.AzureName("snapshot-scheduler"),
                "../SoloDevBoard.Functions/SoloDevBoard.Functions.csproj")
            .WithHostStorage(storage)
            .WithReference(snapshots)
            .WithReference(hostedSignIns)
            .WithReference(collectionSchedule)
            .WithReference(queues)
            .WaitFor(storage);
        functions = HostedSnapshotSchedulerBindings.Apply(host, functions);

        var authSecretsVault = TryAddLocalAuthSecretsVault(host);
        functions = WireLocalHostedGitHubAppClientSecret(host, functions, authSecretsVault);

        var app = host.CreateWebApp();
        app = app.WithReference(snapshots).WithReference(hostedSignIns).WaitFor(snapshots);
        app = WireLocalHostedSignInAuth(host, app, authSecretsVault);
        app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
        host.SetSnapshotScheduler(functions);
        host.SetApp(app);
    }

    /// <summary>Adds Azure deploy infrastructure for hosted sign-in, including operator snapshot storage.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigurePublishInfrastructure(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var aca = host.Builder.AddAzureContainerAppEnvironment(host.Naming.AzureName("aca"));
        host.SetContainerAppEnvironment(aca);

        var storage = host.Builder.AddAzureStorage("operator-snapshot-storage");
        var snapshots = storage.AddBlobContainer("operator-snapshots", "operator-snapshots");
        var hostedSignIns = storage.AddBlobContainer("hosted-sign-ins", "hosted-sign-ins");
        var collectionSchedule = storage.AddBlobContainer("collection-schedule", "collection-schedule");
        var queues = storage.AddQueues("operator-snapshot-queues");

        var functions = host.Builder
            .AddAzureFunctionsProject(
                host.Naming.AzureName("snapshot-scheduler"),
                "../SoloDevBoard.Functions/SoloDevBoard.Functions.csproj")
            .WithHostStorage(storage)
            .WithReference(snapshots)
            .WithReference(hostedSignIns)
            .WithReference(collectionSchedule)
            .WithReference(queues);
        functions = HostedSnapshotSchedulerBindings.Apply(host, functions);

        functions.WithRoleAssignments(
            storage,
            StorageBuiltInRole.StorageBlobDataContributor,
            StorageBuiltInRole.StorageQueueDataContributor,
            StorageBuiltInRole.StorageTableDataContributor,
            StorageBuiltInRole.StorageAccountContributor);

        var app = host.CreateWebApp().WithReference(snapshots).WithReference(hostedSignIns);
        host.SetSnapshotScheduler(functions);
        host.SetApp(app);
    }

    private static IResourceBuilder<AzureKeyVaultResource>? TryAddLocalAuthSecretsVault(SoloDevBoardAppHost host)
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

        if (!needVaultForPat && !needVaultForClientSecret)
        {
            return null;
        }

        // Path B local dev: operators grant Key Vault Secrets User manually (see docs/azure-key-vault-setup.md).
        // Skip WithRoleAssignments here to avoid auth-secrets-roles ARM provisioning on every aspire start.
        return host.Builder.AddAzureKeyVault("auth-secrets")
            .AsExisting(parameters.AuthSecretsVaultName, parameters.AuthSecretsVaultResourceGroup);
    }

    private static IResourceBuilder<DotnetProjectResource> WireLocalHostedSignInAuth(
        SoloDevBoardAppHost host,
        IResourceBuilder<DotnetProjectResource> app,
        IResourceBuilder<AzureKeyVaultResource>? authSecretsVault)
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

        if (authSecretsVault is not null)
        {
            app = app.WithReference(authSecretsVault);

            if (needVaultForPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"));
            }
            else if (useLocalPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
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
            if (useLocalPat)
            {
                app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", parameters.GithubPat);
            }

            if (useLocalClientSecret)
            {
                app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", parameters.GhAppClientSecret);
            }
        }

        return app;
    }

    internal static IResourceBuilder<AzureFunctionsProjectResource> WireLocalHostedGitHubAppClientSecret(
        SoloDevBoardAppHost host,
        IResourceBuilder<AzureFunctionsProjectResource> functions,
        IResourceBuilder<AzureKeyVaultResource>? authSecretsVault)
    {
        var configuration = host.Builder.Configuration;
        var parameters = host.Parameters;

        var hostedSignIn = AppHostDeployParameterResolver.IsHostedSignInEnabled(configuration);
        if (!hostedSignIn)
        {
            return functions;
        }

        var resolvedGhAppClientSecret = AppHostDeployParameterResolver.Resolve(configuration, "gh-app-client-secret", string.Empty);
        var useLocalClientSecret = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGhAppClientSecret);
        var vaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(configuration);
        var needVaultForClientSecret = !useLocalClientSecret && vaultConfigured;

        if (needVaultForClientSecret && authSecretsVault is not null)
        {
            functions = functions.WithReference(authSecretsVault);
            functions = functions.WithEnvironment(
                "GitHubAuth__HostedGitHubAppClientSecret",
                authSecretsVault.GetSecret("gh-app-client-secret"));
        }
        else if (useLocalClientSecret)
        {
            functions = functions.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", parameters.GhAppClientSecret);
        }

        return functions;
    }
}
