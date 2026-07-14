using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Validates GitHub authentication configuration for the active mode.</summary>
public static class GitHubAuthConfigurationValidation
{
    /// <summary>Validates configuration for the active authentication mode.</summary>
    /// <param name="authOptions">GitHub authentication options.</param>
    /// <param name="admissionOptions">Hosted admission control options.</param>
    /// <exception cref="InvalidOperationException">Thrown when required settings for the active mode are missing.</exception>
    public static void Validate(GitHubAuthOptions authOptions, HostedAdmissionControlOptions admissionOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);
        ArgumentNullException.ThrowIfNull(admissionOptions);

        if (authOptions.HostedSignInEnabled)
        {
            ValidateHostedSignInMode(authOptions, admissionOptions);
            return;
        }

        ValidatePatMode(authOptions);
    }

    private static void ValidatePatMode(GitHubAuthOptions authOptions)
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(authOptions.PersonalAccessToken))
        {
            throw CreateConfigurationException(
                "PAT mode is active because GitHubAuth:HostedSignInEnabled is false, but no personal access token is configured. " +
                "Set the AppHost parameter 'github-pat' in the Aspire dashboard (or GitHubAuth:PersonalAccessToken for the legacy dotnet run path).");
        }

        EnsureInactiveForPatMode(authOptions.HostedGitHubAppClientId, "github-app-client-id");
        EnsureInactiveForPatMode(authOptions.HostedGitHubAppClientSecret, "github-app-client-secret");
    }

    private static void ValidateHostedSignInMode(
        GitHubAuthOptions authOptions,
        HostedAdmissionControlOptions admissionOptions)
    {
        EnsureInactiveForHostedMode(authOptions.PersonalAccessToken, "github-pat");

        var missingParameters = new List<string>();

        if (!AuthConfigurationPlaceholders.IsConfigured(authOptions.HostedGitHubAppClientId))
        {
            missingParameters.Add("github-app-client-id");
        }

        if (!AuthConfigurationPlaceholders.IsConfigured(authOptions.HostedGitHubAppClientSecret))
        {
            missingParameters.Add("github-app-client-secret");
        }

        if (missingParameters.Count > 0)
        {
            throw CreateConfigurationException(
                "Hosted sign-in is enabled but GitHub App OAuth credentials are incomplete. " +
                $"Set the AppHost parameter(s): {string.Join(", ", missingParameters)}. " +
                "Set inactive PAT mode parameters to '-'.");
        }

        if (!admissionOptions.Enabled)
        {
            return;
        }

        if (HostedAdmissionAllowList.HasConfiguredEntries(admissionOptions.AllowedUserLogins)
            || HostedAdmissionAllowList.HasConfiguredEntries(admissionOptions.AllowedOrganisationLogins))
        {
            return;
        }

        throw CreateConfigurationException(
            "Hosted sign-in is enabled with admission control, but no allow-list entries are configured. " +
            "Set 'allowed-user-logins' and/or 'allowed-org-logins' with GitHub logins. " +
            "Use '-' on the allow-list parameter you are not using.");
    }

    private static void EnsureInactiveForPatMode(string? value, string parameterName)
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(value))
        {
            return;
        }

        throw CreateConfigurationException(
            $"PAT mode is active but AppHost parameter '{parameterName}' is set to a value. " +
            $"Set '{parameterName}' to '-' for PAT mode.");
    }

    private static void EnsureInactiveForHostedMode(string? value, string parameterName)
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(value))
        {
            return;
        }

        throw CreateConfigurationException(
            $"Hosted sign-in is active but AppHost parameter '{parameterName}' is set to a value. " +
            $"Set '{parameterName}' to '-' for hosted sign-in mode.");
    }

    private static InvalidOperationException CreateConfigurationException(string message) =>
        new(message);
}
