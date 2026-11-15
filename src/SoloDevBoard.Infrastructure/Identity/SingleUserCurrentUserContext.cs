using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Phase 2 single-user implementation of <see cref="ICurrentUserContext"/>.
/// Reads the configured PAT from <see cref="GitHubAuthOptions"/> until Phase 6 replaces
/// this adapter with a per-request authenticated user context (ADR-0007).
/// </summary>
public sealed class SingleUserCurrentUserContext(
    IOptions<GitHubAuthOptions> authOptions,
    ResolvedPatOwnerLogin resolvedPatOwnerLogin) : ICurrentUserContext
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly ResolvedPatOwnerLogin _resolvedPatOwnerLogin = resolvedPatOwnerLogin ?? throw new ArgumentNullException(nameof(resolvedPatOwnerLogin));

    /// <inheritdoc/>
    public string OwnerLogin
    {
        get
        {
            if (AuthConfigurationPlaceholders.IsConfigured(_authOptions.OwnerLogin))
            {
                return _authOptions.OwnerLogin;
            }

            if (AuthConfigurationPlaceholders.IsConfigured(_resolvedPatOwnerLogin.Value))
            {
                return _resolvedPatOwnerLogin.Value!;
            }

            throw new InvalidOperationException(
                "GitHub owner login is not configured and could not be resolved from the personal access token. " +
                $"Set '{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.OwnerLogin)}' explicitly, " +
                $"or configure a valid '{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.PersonalAccessToken)}'.");
        }
    }

    /// <inheritdoc/>
    public string GetAccessToken()
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.PersonalAccessToken))
        {
            throw new InvalidOperationException(
                $"GitHub personal access token is not configured. Check configuration key '{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.PersonalAccessToken)}'.");
        }

        return _authOptions.PersonalAccessToken;
    }
}
