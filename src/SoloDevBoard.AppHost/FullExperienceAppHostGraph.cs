using Aspire.Hosting.Dotnet;

/// <summary>
/// Hosted sign-in AppHost graph: web compute with GitHub App OAuth and admission control.
/// Inventory SQL, Functions, and storage are deferred until Azure SQL is available on the subscription (ADR-0046).
/// </summary>
internal static class FullExperienceAppHostGraph
{
    /// <summary>Configures local run for hosted sign-in with optional dev Key Vault.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigureLocalRun(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var app = host.CreateWebApp();
        app = WireLocalHostedSignInAuth(host, app);
        app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
        host.SetApp(app);
    }

    /// <summary>Adds Azure deploy infrastructure for hosted sign-in (web compute only).</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void ConfigurePublishInfrastructure(SoloDevBoardAppHost host) =>
        LeanAppHostGraph.ConfigurePublishInfrastructure(host);

    private static IResourceBuilder<DotnetProjectResource> WireLocalHostedSignInAuth(
        SoloDevBoardAppHost host,
        IResourceBuilder<DotnetProjectResource> app)
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

        if (needVaultForPat || needVaultForClientSecret)
        {
            var authSecretsVault = host.Builder.AddAzureKeyVault("auth-secrets")
                .AsExisting(parameters.AuthSecretsVaultName, parameters.AuthSecretsVaultResourceGroup);

            // Path B local dev: operators grant Key Vault Secrets User manually (see docs/azure-key-vault-setup.md).
            // Skip WithRoleAssignments here to avoid auth-secrets-roles ARM provisioning on every aspire start.
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
}
