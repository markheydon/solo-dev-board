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

var hostedSignInEnabled = builder.AddParameter("hosted-sign-in-enabled")
    .WithDescription("Enable GitHub App hosted sign-in at /auth/sign-in. When false, PAT mode is used (default).");

var githubPat = builder.AddParameter("gh-pat", secret: true)
    .WithDescription("GitHub PAT for PAT mode. Set to '-' via the dashboard for hosted sign-in. Your GitHub login is resolved automatically from the token.");

var ghAppClientId = builder.AddParameter("gh-app-client-id")
    .WithDescription("GitHub App OAuth client ID for hosted sign-in. Set to '-' for PAT mode.");

var ghAppClientSecret = builder.AddParameter("gh-app-client-secret", secret: true)
    .WithDescription("GitHub App OAuth client secret for hosted sign-in. Set to '-' via the dashboard for hosted sign-in.");

var hostedAdmissionEnabled = builder.AddParameter("hosted-admission-enabled")
    .WithDescription("Enable deny-by-default admission control for hosted sign-in.");

var allowedUserLogins = builder.AddParameter("allowed-user-logins")
    .WithDescription("Comma-separated GitHub user logins for hosted admission. Use '-' when using allowed-org-logins instead, or in PAT mode.");

var allowedOrgLogins = builder.AddParameter("allowed-org-logins")
    .WithDescription("Comma-separated GitHub organisation logins for hosted admission. Use '-' when using allowed-user-logins instead, or in PAT mode.");

var app = builder.AddProject<Projects.SoloDevBoard_App>(AzureName("app"))
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
        .WithDescription("Optional absolute HTTPS base URI for hosted OAuth callbacks (for example https://staging.solodevboard.app). Use '-' to use the Aspire-provisioned endpoint.");

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

        aca.WithAzureContainerRegistry(acr);
    }

    var customDomain = builder.AddParameter("custom-domain")
        .WithDescription("Optional custom hostname for the Container App (for example staging.solodevboard.app). Use '-' to use the Aspire-provisioned FQDN only.");

    var customDomainCertificateName = builder.AddParameter("custom-domain-certificate-name")
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

    var authSecretsVault = builder.AddAzureKeyVault(AzureName("auth-secrets"));

    authSecretsVault.AddSecret("auth-gh-pat", "gh-pat", githubPat);
    authSecretsVault.AddSecret("auth-gh-app-client-secret", "gh-app-client-secret", ghAppClientSecret);

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

    var resolvedCustomDomain = AppHostDeployParameterResolver.Resolve(builder.Configuration, "custom-domain");
    if (AppHostDeployParameterResolver.IsActiveParameterValue(resolvedCustomDomain))
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
    app = app
        .WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat)
        .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", ghAppClientSecret)
        .WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));
}

builder.Build().Run();
