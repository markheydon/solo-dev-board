using Aspire.Hosting.Azure;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.KeyVault;

/// <summary>
/// Azure publish profile: deploy parameters, Key Vault secrets, telemetry, and optional ACR or custom domain.
/// </summary>
internal static class AppHostPublishProfile
{
    /// <summary>Models publish-time infrastructure and deploy bindings.</summary>
    /// <param name="host">AppHost orchestrator.</param>
    public static void Configure(SoloDevBoardAppHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (host.UseHostedSignInPosture)
        {
            HostedSignInAppHostGraph.ConfigurePublishInfrastructure(host);
        }
        else
        {
            LeanAppHostGraph.ConfigurePublishInfrastructure(host);
        }

        Apply(host);
    }

    private static void Apply(SoloDevBoardAppHost host)
    {
        var builder = host.Builder;
        var naming = host.Naming;
        var parameters = host.Parameters;
        var aca = host.ContainerAppEnvironment
            ?? throw new InvalidOperationException("Container App environment must be configured before publish profile.");

        builder.AddParameter("hosted-callback-base-uri")
            .WithDescription(
                "Optional absolute HTTPS base URI for hosted OAuth callbacks (for example https://staging.example.com). Use '-' to use the Aspire-provisioned endpoint.");

        var acrName = builder.AddParameter("acr-name")
            .WithDescription(
                "Optional existing Azure Container Registry resource name. Use with acr-resource-group, or omit both for Aspire's default registry.");

        var acrResourceGroup = builder.AddParameter("acr-resource-group")
            .WithDescription(
                "Resource group that owns the existing ACR. Use with acr-name, or omit both for Aspire's default registry.");

        var resolvedAcrName = AppHostDeployParameterResolver.Resolve(builder.Configuration, "acr-name");
        var resolvedAcrResourceGroup = AppHostDeployParameterResolver.Resolve(builder.Configuration, "acr-resource-group");
        AppHostDeployParameterResolver.EnsurePairOrNeither(
            resolvedAcrName,
            resolvedAcrResourceGroup,
            "acr-name",
            "acr-resource-group");

        if (AppHostDeployParameterResolver.IsActiveParameterValue(resolvedAcrName))
        {
            var acr = builder.AddAzureContainerRegistry("acr")
                .PublishAsExisting(acrName, acrResourceGroup);

            // Cross-RG existing ACR: default ACA Bicep emits an AcrPull role assignment at the wrong scope (BCP139).
            // WithAcrPullIdentity provisions AcrPull via a user-assigned identity module instead (Aspire #11256).
            var acrPullIdentity = builder.AddAzureUserAssignedIdentity(naming.AzureName("acr-pull"))
                .WithRoleAssignments(acr, ContainerRegistryBuiltInRole.AcrPull);

            aca.WithAzureContainerRegistry(acr)
                .WithAcrPullIdentity(acrPullIdentity);
        }

        var resolvedCustomDomain = AppHostDeployParameterResolver.Resolve(builder.Configuration, "custom-domain");
        var resolvedCustomDomainCertificateName = AppHostDeployParameterResolver.Resolve(
            builder.Configuration,
            "custom-domain-certificate-name");

        var customDomain = builder.AddParameter("custom-domain", resolvedCustomDomain)
            .WithDescription(
                "Optional custom hostname for the Container App (for example staging.example.com). Use '-' to use the Aspire-provisioned FQDN only.");

        var customDomainCertificateName = builder.AddParameter(
                "custom-domain-certificate-name",
                resolvedCustomDomainCertificateName)
            .WithDescription(
                "Managed certificate name in the Container Apps environment for the custom domain. Leave '-' on first deploy before the certificate is provisioned.");

        var resolvedHostedSignInEnabled = AppHostDeployParameterResolver.Resolve(
            builder.Configuration,
            "hosted-sign-in-enabled",
            host.UseHostedSignInPosture ? "true" : "false");
        var resolvedHostedAdmissionEnabled = AppHostDeployParameterResolver.Resolve(
            builder.Configuration,
            "hosted-admission-enabled",
            "true");
        var resolvedGhAppClientId = AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-app-client-id");
        var resolvedAllowedUserLogins = AppHostDeployParameterResolver.Resolve(builder.Configuration, "allowed-user-logins");
        var resolvedAllowedOrgLogins = AppHostDeployParameterResolver.Resolve(builder.Configuration, "allowed-org-logins");

        var app = host.App
            .WithEnvironment("GitHubAuth__HostedSignInEnabled", resolvedHostedSignInEnabled)
            .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", resolvedGhAppClientId)
            .WithEnvironment("HostedAdmissionControl__Enabled", resolvedHostedAdmissionEnabled)
            .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", resolvedAllowedUserLogins)
            .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", resolvedAllowedOrgLogins);

        AppHostDeployParameterResolver.EnsureAuthSecretsVaultConfigured(builder.Configuration);

        var authSecretsVault = builder.AddAzureKeyVault(naming.AzureName("auth-secrets"))
            .PublishAsExisting(parameters.AuthSecretsVaultName, parameters.AuthSecretsVaultResourceGroup);

        var appInsights = builder.AddAzureApplicationInsights(naming.AzureName("app-insights"));

        app = app
            .WithRoleAssignments(authSecretsVault, KeyVaultBuiltInRole.KeyVaultSecretsUser)
            .WithReference(authSecretsVault)
            .WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"))
            .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", authSecretsVault.GetSecret("gh-app-client-secret"))
            .WithReference(appInsights);

        var resolvedCallbackBaseUri = AppHostDeployParameterResolver.Resolve(builder.Configuration, "hosted-callback-base-uri");
        if (AppHostDeployParameterResolver.IsActiveParameterValue(resolvedCallbackBaseUri))
        {
            app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", resolvedCallbackBaseUri);
        }
        else
        {
            app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
        }

        if (AppHostDeployParameterResolver.IsActiveParameterValue(resolvedCustomDomain)
            && AppHostDeployParameterResolver.IsActiveParameterValue(resolvedCustomDomainCertificateName))
        {
            app = app.PublishAsAzureContainerApp((_, containerApp) =>
            {
#pragma warning disable ASPIREACADOMAINS001 // ConfigureCustomDomain is preview; required to persist custom domain across deploys.
                containerApp.ConfigureCustomDomain(customDomain, customDomainCertificateName);
#pragma warning restore ASPIREACADOMAINS001
            });
        }

        host.SetApp(app);

        if (host.UseHostedSignInPosture && host.SnapshotScheduler is { } scheduler)
        {
            scheduler = scheduler
                .WithEnvironment("GitHubAuth__HostedSignInEnabled", resolvedHostedSignInEnabled)
                .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", resolvedGhAppClientId)
                .WithEnvironment("HostedAdmissionControl__Enabled", resolvedHostedAdmissionEnabled)
                .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", resolvedAllowedUserLogins)
                .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", resolvedAllowedOrgLogins)
                .WithRoleAssignments(authSecretsVault, KeyVaultBuiltInRole.KeyVaultSecretsUser)
                .WithReference(authSecretsVault)
                .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", authSecretsVault.GetSecret("gh-app-client-secret"))
                .WithReference(appInsights);

            host.SetSnapshotScheduler(scheduler);
        }
    }
}
