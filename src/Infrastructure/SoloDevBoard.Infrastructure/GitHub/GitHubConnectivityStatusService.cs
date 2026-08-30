using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Resolves GitHub connectivity status for PAT-only local trusted mode.</summary>
public sealed class GitHubConnectivityStatusService(
    IOptions<GitHubAuthOptions> authOptions,
    ICurrentUserContext currentUserContext) : IGitHubConnectivityStatusService
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly ICurrentUserContext _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));

    /// <inheritdoc/>
    public Task<GitHubConnectivityStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_authOptions.HostedSignInEnabled)
        {
            return Task.FromResult(new GitHubConnectivityStatusDto(
                IsConnected: false,
                OwnerLogin: null,
                StatusMessage: "Hosted sign-in is enabled."));
        }

        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.PersonalAccessToken))
        {
            return Task.FromResult(new GitHubConnectivityStatusDto(
                IsConnected: false,
                OwnerLogin: null,
                StatusMessage: "GitHub personal access token is not configured."));
        }

        try
        {
            var ownerLogin = _currentUserContext.OwnerLogin;
            return Task.FromResult(new GitHubConnectivityStatusDto(
                IsConnected: true,
                OwnerLogin: ownerLogin,
                StatusMessage: $"Connected as @{ownerLogin}."));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(new GitHubConnectivityStatusDto(
                IsConnected: false,
                OwnerLogin: null,
                StatusMessage: "GitHub is not connected."));
        }
    }
}
