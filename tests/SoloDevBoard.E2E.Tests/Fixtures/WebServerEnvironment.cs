namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Environment variables for the SoloDevBoard web server in E2E runs.
/// </summary>
public static class WebServerEnvironment
{
    /// <summary>
    /// Builds the process environment for the Blazor app under test.
    /// </summary>
    /// <param name="authMode">Authentication mode to configure.</param>
    /// <returns>Environment variable name/value pairs.</returns>
    public static IReadOnlyDictionary<string, string> Create(E2eAuthModeValue? authMode = null)
    {
        var baseEnv = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ASPNETCORE_URLS"] = SoloDevBoardWebApplicationFixture.BaseUrl,
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
        };

        authMode ??= E2eAuthMode.Current;

        if (authMode == E2eAuthModeValue.Hosted)
        {
            return new Dictionary<string, string>(baseEnv, StringComparer.Ordinal)
            {
                ["ASPNETCORE_URLS"] = baseEnv["ASPNETCORE_URLS"],
                ["ASPNETCORE_ENVIRONMENT"] = baseEnv["ASPNETCORE_ENVIRONMENT"],
                ["GitHubAuth__PersonalAccessToken"] = "-",
                ["GitHubAuth__HostedSignInEnabled"] = "true",
                ["GitHubAuth__HostedGitHubAppClientId"] = "ci-e2e-hosted-client-id",
                ["GitHubAuth__HostedGitHubAppClientSecret"] = "ci-e2e-hosted-client-secret",
                ["HostedAdmissionControl__Enabled"] = "true",
                ["HostedAdmissionControl__AllowedUserLogins"] = "ci-test-user",
                ["HostedAdmissionControl__AllowedOrganisationLogins"] = "-",
            };
        }

        return new Dictionary<string, string>(baseEnv, StringComparer.Ordinal)
        {
            ["ASPNETCORE_URLS"] = baseEnv["ASPNETCORE_URLS"],
            ["ASPNETCORE_ENVIRONMENT"] = baseEnv["ASPNETCORE_ENVIRONMENT"],
            ["GitHubAuth__PersonalAccessToken"] = "ci-e2e-placeholder",
            ["GitHubAuth__OwnerLogin"] = "ci-test-user",
            ["GitHubAuth__HostedSignInEnabled"] = "false",
            ["HostedAdmissionControl__Enabled"] = "false",
        };
    }
}
