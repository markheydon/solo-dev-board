using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Phase 2 single-user implementation of <see cref="ICurrentUserContext"/>.
/// Reads the configured PAT from <see cref="GitHubAuthOptions"/> until Phase 6 replaces
/// this adapter with a per-request authenticated user context (ADR-0007).
/// </summary>
public sealed class SingleUserCurrentUserContext(IOptions<GitHubAuthOptions> authOptions) : ICurrentUserContext
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    public string GetAccessToken()
    {
        if (string.IsNullOrWhiteSpace(_authOptions.PersonalAccessToken))
        {
            throw new InvalidOperationException(
                $"GitHub personal access token is not configured. Check configuration key '{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.PersonalAccessToken)}'.");
        }

        return _authOptions.PersonalAccessToken;
    }
}