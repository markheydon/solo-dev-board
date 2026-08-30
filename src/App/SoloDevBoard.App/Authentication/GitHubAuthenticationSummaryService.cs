using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.App.Authentication;

/// <summary>Resolves GitHub authentication mode and identity details from hosted or PAT configuration.</summary>
public sealed class GitHubAuthenticationSummaryService(
    IOptions<GitHubAuthOptions> authOptions,
    AuthenticationStateProvider authenticationStateProvider,
    IGitHubConnectivityStatusService connectivityStatusService) : IGitHubAuthenticationSummaryService
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
    private readonly IGitHubConnectivityStatusService _connectivityStatusService = connectivityStatusService ?? throw new ArgumentNullException(nameof(connectivityStatusService));

    /// <inheritdoc/>
    public async Task<GitHubAuthenticationSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (_authOptions.HostedSignInEnabled)
        {
            var authenticationState = await _authenticationStateProvider
                .GetAuthenticationStateAsync()
                .ConfigureAwait(false);

            var login = ResolveHostedLogin(authenticationState.User);

            return new GitHubAuthenticationSummary(
                ModeLabel: "Hosted sign-in",
                IdentityLabel: "Signed in as",
                GitHubLogin: login);
        }

        var status = await _connectivityStatusService.GetStatusAsync(cancellationToken).ConfigureAwait(false);

        return new GitHubAuthenticationSummary(
            ModeLabel: "PAT-only local trusted mode",
            IdentityLabel: status.IsConnected ? "Connected as" : "GitHub identity",
            GitHubLogin: status.OwnerLogin);
    }

    private string? ResolveHostedLogin(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst(_authOptions.HostedOwnerLoginClaimType)?.Value
            ?? user.Identity.Name;
    }
}
