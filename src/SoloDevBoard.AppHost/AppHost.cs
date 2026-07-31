using Azure.Provisioning.KeyVault;

var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

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

var app = builder.AddProject<Projects.SoloDevBoard_App>("app")
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
    builder.AddParameter("shared-acr-name")
        .WithDescription("Optional shared Azure Container Registry name. Use '-' to let Aspire provision a per-deployment registry.");

    builder.AddParameter("shared-acr-resource-group")
        .WithDescription("Resource group containing the shared registry. Required when shared-acr-name is set.");

    var acrNameValue = Environment.GetEnvironmentVariable("Parameters__shared_acr_name")
        ?? builder.Configuration["Parameters:shared-acr-name"]
        ?? builder.Configuration["Parameters:shared_acr_name"]
        ?? "-";
    var acrRgValue = Environment.GetEnvironmentVariable("Parameters__shared_acr_resource_group")
        ?? builder.Configuration["Parameters:shared-acr-resource-group"]
        ?? builder.Configuration["Parameters:shared_acr_resource_group"]
        ?? "-";

    if (acrNameValue is not ("-" or ""))
    {
        if (acrRgValue is "-" or "")
        {
            throw new InvalidOperationException(
                "shared-acr-resource-group must be set when shared-acr-name is configured.");
        }

        var acr = builder.AddAzureContainerRegistry("shared-acr")
            .PublishAsExisting(acrNameValue, acrRgValue);

        aca = aca.WithAzureContainerRegistry(acr);
    }

    var authSecretsVault = builder.AddAzureKeyVault("auth-secrets");

    authSecretsVault.AddSecret("auth-gh-pat", "gh-pat", githubPat);
    authSecretsVault.AddSecret("auth-gh-app-client-secret", "gh-app-client-secret", ghAppClientSecret);

    app = app
        .WithRoleAssignments(authSecretsVault, KeyVaultBuiltInRole.KeyVaultSecretsUser)
        .WithReference(authSecretsVault)
        .WithEnvironment("GitHubAuth__PersonalAccessToken", authSecretsVault.GetSecret("gh-pat"))
        .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", authSecretsVault.GetSecret("gh-app-client-secret"))
        .WithReference(builder.AddAzureApplicationInsights("app-insights"));
}
else
{
    app = app
        .WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat)
        .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", ghAppClientSecret);
}

app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));

builder.Build().Run();
