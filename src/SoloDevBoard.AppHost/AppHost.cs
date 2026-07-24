var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca");

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
    .WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", ghAppClientId)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", ghAppClientSecret)
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
    app = app.WithReference(builder.AddAzureApplicationInsights("app-insights"));
}

app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));

builder.Build().Run();
