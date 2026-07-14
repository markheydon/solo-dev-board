const string DisabledPlaceholder = "__disabled__";

var builder = DistributedApplication.CreateBuilder(args);

var hostedSignInEnabled = builder.AddParameter("hosted-sign-in-enabled")
    .WithDescription("Enable GitHub App hosted sign-in at /auth/sign-in. When false, PAT mode is used (default).");

var githubPat = builder.AddParameter("github-pat", DisabledPlaceholder, secret: true)
    .WithDescription("GitHub PAT (repo, read:org, workflow, read:project scopes). Required for PAT mode. Your GitHub login is resolved automatically from the token. Leave as __disabled__ for hosted sign-in.");

var githubAppClientId = builder.AddParameter("github-app-client-id", DisabledPlaceholder)
    .WithDescription("GitHub App OAuth client ID. Set when hosted-sign-in-enabled is true. Leave as __disabled__ for PAT mode.");

var githubAppClientSecret = builder.AddParameter("github-app-client-secret", DisabledPlaceholder, secret: true)
    .WithDescription("GitHub App OAuth client secret. Set when hosted-sign-in-enabled is true. Leave as __disabled__ for PAT mode.");

var hostedAdmissionEnabled = builder.AddParameter("hosted-admission-enabled")
    .WithDescription("Enable deny-by-default admission control for hosted sign-in.");

var allowedUserLogins = builder.AddParameter("allowed-user-logins", DisabledPlaceholder)
    .WithDescription("Comma-separated GitHub user logins permitted for hosted access. Replace __disabled__ with real logins when enabling hosted sign-in.");

var allowedOrgLogins = builder.AddParameter("allowed-org-logins", DisabledPlaceholder)
    .WithDescription("Comma-separated GitHub organisation logins permitted for hosted access. Replace __disabled__ with real logins when enabling hosted sign-in.");

var app = builder.AddProject<Projects.SoloDevBoard_App>("app")
    .WithExternalHttpEndpoints()
    .WithEnvironment("GitHubAuth__HostedSignInEnabled", hostedSignInEnabled)
    .WithEnvironment("GitHubAuth__PersonalAccessToken", githubPat)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", githubAppClientId)
    .WithEnvironment("GitHubAuth__HostedGitHubAppClientSecret", githubAppClientSecret)
    .WithEnvironment("HostedAdmissionControl__Enabled", hostedAdmissionEnabled)
    .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", allowedUserLogins)
    .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", allowedOrgLogins);

app.WithEnvironment("GitHubAuth__HostedSignInCallbackBaseUri", app.GetEndpoint("https"));

builder.Build().Run();
