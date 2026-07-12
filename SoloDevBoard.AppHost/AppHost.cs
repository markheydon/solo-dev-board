var builder = DistributedApplication.CreateBuilder(args);

var hostedSignInEnabled = builder.AddParameter("hosted-sign-in-enabled")
    .WithDescription("Enable GitHub App hosted sign-in at /auth/sign-in. When false, PAT mode is used.");

var githubOwnerLogin = builder.AddParameter("github-owner-login")
    .WithDescription("GitHub login for PAT mode. Required when hosted sign-in is disabled.");

var githubPat = builder.AddParameter("github-pat", secret: true)
    .WithDescription("GitHub PAT with repo, read:org, workflow, and read:project scopes. Required when hosted sign-in is disabled.");

var githubAppClientId = builder.AddParameter("github-app-client-id")
    .WithDescription("GitHub App OAuth client ID. Required when hosted sign-in is enabled.");

var githubAppClientSecret = builder.AddParameter("github-app-client-secret", secret: true)
    .WithDescription("GitHub App OAuth client secret. Required when hosted sign-in is enabled.");

var hostedAdmissionEnabled = builder.AddParameter("hosted-admission-enabled")
    .WithDescription("Enable deny-by-default admission control for hosted sign-in.");

var allowedUserLogins = builder.AddParameter("allowed-user-logins", "-")
    .WithDescription("Comma-separated GitHub user logins permitted for hosted access. Use '-' when hosted sign-in is disabled.");

var allowedOrgLogins = builder.AddParameter("allowed-org-logins", "-")
    .WithDescription("Comma-separated GitHub organisation logins permitted for hosted access. Use '-' when hosted sign-in is disabled.");

var app = builder.AddProject<Projects.SoloDevBoard_App>("app")
    .WithExternalHttpEndpoints()
    .WithEnvironment("GitHubAuth__HostedSignInEnabled", hostedSignInEnabled)
    .WithEnvironment("GitHubAuth__OwnerLogin", githubOwnerLogin)
    .WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", githubAppClientId)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", githubAppClientSecret)
    .WithEnvironment("HostedAdmissionControl__Enabled", hostedAdmissionEnabled)
    .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", allowedUserLogins)
    .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", allowedOrgLogins);

app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));

builder.Build().Run();
