using Aspire.Hosting.Azure;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.KeyVault;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire --environment does not suffix Azure names (DEC-021). Staging uses distinct
// resource names so it does not overwrite Production when both tiers share an app RG.
// Production keeps the original names so an existing production Container App is not recreated.
string AzureName(string resourceName) =>
    string.Equals(builder.Environment.EnvironmentName, "Staging", StringComparison.OrdinalIgnoreCase)
        ? $"{resourceName}-staging"
        : resourceName;

var aca = builder.AddAzureContainerAppEnvironment(AzureName("aca"));

var resolvedAuthSecretsVaultName = AppHostDeployParameterResolver.Resolve(builder.Configuration, "auth-secrets-vault-name");
var resolvedAuthSecretsVaultResourceGroup = AppHostDeployParameterResolver.Resolve(
    builder.Configuration,
    "auth-secrets-vault-resource-group");
if (!AppHostDeployParameterResolver.IsActiveParameterValue(resolvedAuthSecretsVaultResourceGroup))
{
    resolvedAuthSecretsVaultResourceGroup = AppHostDeployParameterResolver.ResolveAuthSecretsVaultResourceGroup(builder.Configuration);
}

var authSecretsVaultName = builder.AddParameter("auth-secrets-vault-name", resolvedAuthSecretsVaultName)
    .WithDescription(
        "Azure Key Vault name for auth secret fallback (local Path B) and deploy. " +
        "Use '-' for local Path A only. Staging defaults to auth-secrets-staging; Production to auth-secrets.");

var authSecretsVaultResourceGroup = builder.AddParameter("auth-secrets-vault-resource-group", resolvedAuthSecretsVaultResourceGroup)
    .WithDescription(
        "Resource group that owns the auth-secrets vault. Use '-' to inherit Azure__ResourceGroup at deploy or local Path B.");

var pathBAuthSecretsVaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(builder.Configuration);

var hostedSignInEnabled = builder.AddParameter("hosted-sign-in-enabled")
    .WithDescription("Enable GitHub App hosted sign-in at /auth/sign-in. When false, PAT mode is used (default).");

var githubPat = pathBAuthSecretsVaultConfigured
    ? builder.AddParameter(
        "gh-pat",
        AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-pat", "-"),
        secret: true)
    : builder.AddParameter("gh-pat", secret: true);
githubPat = githubPat.WithDescription(
    "GitHub PAT for PAT mode. Set to '-' via the dashboard for hosted sign-in. Your GitHub login is resolved automatically from the token.");

var ghAppClientId = builder.AddParameter("gh-app-client-id")
    .WithDescription("GitHub App OAuth client ID for hosted sign-in. Set to '-' for PAT mode.");

var ghAppClientSecret = pathBAuthSecretsVaultConfigured
    ? builder.AddParameter(
        "gh-app-client-secret",
        AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-app-client-secret", "-"),
        secret: true)
    : builder.AddParameter("gh-app-client-secret", secret: true);
ghAppClientSecret = ghAppClientSecret.WithDescription(
    "GitHub App OAuth client secret for hosted sign-in. Set to '-' via the dashboard for PAT mode.");

var hostedAdmissionEnabled = builder.AddParameter("hosted-admission-enabled")
    .WithDescription("Enable deny-by-default admission control for hosted sign-in.");

var allowedUserLogins = builder.AddParameter("allowed-user-logins")
    .WithDescription("Comma-separated GitHub user logins for hosted admission. Use '-' when using allowed-org-logins instead, or in PAT mode.");

var allowedOrgLogins = builder.AddParameter("allowed-org-logins")
    .WithDescription("Comma-separated GitHub organisation logins for hosted admission. Use '-' when using allowed-user-logins instead, or in PAT mode.");

#pragma warning disable ASPIREDOTNETPROJECT001
var app = builder.AddDotnetProject(AzureName("app"), "../App/SoloDevBoard.App/SoloDevBoard.App.csproj");
#pragma warning restore ASPIREDOTNETPROJECT001

app = app
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithEnvironment("GitHubAuth__HostedSignInEnabled", hostedSignInEnabled)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", ghAppClientId)
    .WithEnvironment("HostedAdmissionControl__Enabled", hostedAdmissionEnabled)
    .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", allowedUserLogins)
    .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", allowedOrgLogins)
    .PublishAsAzureContainerApp((_, containerApp) =>
    {
        containerApp.Template.Scale.MinReplicas = 0;
        containerApp.Template.Scale.MaxReplicas = 1;
    });

if (builder.ExecutionContext.IsPublishMode)
{
    builder.AddParameter("hosted-callback-base-uri")
        .WithDescription("Optional absolute HTTPS base URI for hosted OAuth callbacks (for example https://staging.example.com). Use '-' to use the Aspire-provisioned endpoint.");

    var acrName = builder.AddParameter("acr-name")
        .WithDescription("Optional existing Azure Container Registry resource name. Use with acr-resource-group, or omit both for Aspire's default registry.");

    var acrResourceGroup = builder.AddParameter("acr-resource-group")
        .WithDescription("Resource group that owns the existing ACR. Use with acr-name, or omit both for Aspire's default registry.");

    var resolvedAcrName = AppHostDeployParameterResolver.Resolve(builder.Configuration, "acr-name");
    var resolvedAcrResourceGroup = AppHostDeployParameterResolver.Resolve(builder.Configuration, "acr-resource-group");
    AppHostDeployParameterResolver.EnsurePairOrNeither(resolvedAcrName, resolvedAcrResourceGroup, "acr-name", "acr-resource-group");

    if (AppHostDeployParameterResolver.IsActiveParameterValue(resolvedAcrName))
    {
        var acr = builder.AddAzureContainerRegistry("acr")
            .PublishAsExisting(acrName, acrResourceGroup);

        // Cross-RG existing ACR: default ACA Bicep emits an AcrPull role assignment at the wrong scope (BCP139).
        // WithAcrPullIdentity provisions AcrPull via a user-assigned identity module instead (Aspire #11256).
        var acrPullIdentity = builder.AddAzureUserAssignedIdentity(AzureName("acr-pull"))
            .WithRoleAssignments(acr, ContainerRegistryBuiltInRole.AcrPull);

        aca.WithAzureContainerRegistry(acr)
            .WithAcrPullIdentity(acrPullIdentity);
    }

    var resolvedCustomDomain = AppHostDeployParameterResolver.Resolve(builder.Configuration, "custom-domain");
    var resolvedCustomDomainCertificateName = AppHostDeployParameterResolver.Resolve(builder.Configuration, "custom-domain-certificate-name");

    var customDomain = builder.AddParameter("custom-domain", resolvedCustomDomain)
        .WithDescription("Optional custom hostname for the Container App (for example staging.example.com). Use '-' to use the Aspire-provisioned FQDN only.");

    var customDomainCertificateName = builder.AddParameter("custom-domain-certificate-name", resolvedCustomDomainCertificateName)
        .WithDescription("Managed certificate name in the Container Apps environment for the custom domain. Leave '-' on first deploy before the certificate is provisioned.");

    var resolvedHostedSignInEnabled = AppHostDeployParameterResolver.Resolve(builder.Configuration, "hosted-sign-in-enabled", "true");
    var resolvedHostedAdmissionEnabled = AppHostDeployParameterResolver.Resolve(builder.Configuration, "hosted-admission-enabled", "true");
    var resolvedGhAppClientId = AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-app-client-id");
    var resolvedAllowedUserLogins = AppHostDeployParameterResolver.Resolve(builder.Configuration, "allowed-user-logins");
    var resolvedAllowedOrgLogins = AppHostDeployParameterResolver.Resolve(builder.Configuration, "allowed-org-logins");

    app = app
        .WithEnvironment("GitHubAuth__HostedSignInEnabled", resolvedHostedSignInEnabled)
        .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", resolvedGhAppClientId)
        .WithEnvironment("HostedAdmissionControl__Enabled", resolvedHostedAdmissionEnabled)
        .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", resolvedAllowedUserLogins)
        .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", resolvedAllowedOrgLogins);

    AppHostDeployParameterResolver.EnsureAuthSecretsVaultConfigured(builder.Configuration);

    var authSecretsVault = builder.AddAzureKeyVault(AzureName("auth-secrets"))
        .PublishAsExisting(authSecretsVaultName, authSecretsVaultResourceGroup);

    app = app
        .WithRoleAssignments(authSecretsVault, KeyVaultBuiltInRole.KeyVaultSecretsUser)
        .WithReference(authSecretsVault)
        .WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"))
        .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", authSecretsVault.GetSecret("gh-app-client-secret"))
        .WithReference(builder.AddAzureApplicationInsights(AzureName("app-insights")));

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
}
else
{
    var hostedSignIn = AppHostDeployParameterResolver.IsHostedSignInEnabled(builder.Configuration);
    var resolvedGithubPat = AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-pat", string.Empty);
    var resolvedGhAppClientSecret = AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-app-client-secret", string.Empty);
    var useLocalPat = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGithubPat);
    var useLocalClientSecret = AppHostDeployParameterResolver.IsActiveParameterValue(resolvedGhAppClientSecret);
    var vaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(builder.Configuration);

    var needVaultForPat = !hostedSignIn && !useLocalPat && vaultConfigured;
    var needVaultForClientSecret = hostedSignIn && !useLocalClientSecret && vaultConfigured;

    if (needVaultForPat || needVaultForClientSecret)
    {
        var authSecretsVault = builder.AddAzureKeyVault("auth-secrets")
            .AsExisting(authSecretsVaultName, authSecretsVaultResourceGroup);

        // Path B local dev: operators grant Key Vault Secrets User manually (see docs/azure-key-vault-setup.md).
        // Skip WithRoleAssignments here to avoid auth-secrets-roles ARM provisioning on every aspire start.
        app = app.WithReference(authSecretsVault);

        if (needVaultForPat)
        {
            app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"));
        }
        else if (useLocalPat)
        {
            app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat);
        }

        if (needVaultForClientSecret)
        {
            app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", authSecretsVault.GetSecret("gh-app-client-secret"));
        }
        else if (useLocalClientSecret)
        {
            app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", ghAppClientSecret);
        }
    }
    else
    {
        if (useLocalPat)
        {
            app = app.WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat);
        }

        if (useLocalClientSecret)
        {
            app = app.WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", ghAppClientSecret);
        }
    }

    app = app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
}

builder.Build().Run();
