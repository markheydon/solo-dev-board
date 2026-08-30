using SoloDevBoard.Application.Authentication;
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
                "Set the AppHost parameter 'gh-pat' in the Aspire dashboard (or GitHubAuth:PersonalAccessToken for the legacy dotnet run path).");
        }
    }

    private static void ValidateHostedSignInMode(
        GitHubAuthOptions authOptions,
        HostedAdmissionControlOptions admissionOptions)
    {
        var missingParameters = new List<string>();

        if (!AuthConfigurationPlaceholders.IsConfigured(authOptions.HostedGitHubAppClientId))
        {
            missingParameters.Add("gh-app-client-id");
        }

        if (!AuthConfigurationPlaceholders.IsConfigured(authOptions.HostedGitHubAppClientSecret))
        {
            missingParameters.Add("gh-app-client-secret");
        }

        if (missingParameters.Count > 0)
        {
            throw CreateConfigurationException(
                "Hosted sign-in is enabled but GitHub App OAuth credentials are incomplete. " +
                $"Set the AppHost parameter(s): {string.Join(", ", missingParameters)}.");
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

    private static InvalidOperationException CreateConfigurationException(string message) =>
        new(message);
}
