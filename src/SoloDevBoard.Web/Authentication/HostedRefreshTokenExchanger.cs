using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Web.Authentication;

/// <inheritdoc/>
internal sealed class HostedRefreshTokenExchanger(HostedGitHubAuthGateway gateway) : IHostedRefreshTokenExchanger
{
    private readonly HostedGitHubAuthGateway _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

    /// <inheritdoc/>
    public async Task<HostedRefreshTokenExchangeResult> ExchangeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = new HostedGitHubAuthSession(
                OwnerLogin: string.Empty,
                AccessToken: string.Empty,
                InstallationId: null,
                TokenExpiresAtUtc: null,
                OrganisationLogins: [],
                RefreshToken: refreshToken,
                RefreshTokenExpiresAtUtc: null);

            var refreshed = await _gateway.RefreshSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return new HostedRefreshTokenExchangeResult(
                Succeeded: true,
                AccessToken: refreshed.AccessToken,
                RefreshToken: refreshed.RefreshToken,
                RefreshTokenExpiresAtUtc: refreshed.RefreshTokenExpiresAtUtc);
        }
        catch (Exception)
        {
            return new HostedRefreshTokenExchangeResult(false, string.Empty, refreshToken, null);
        }
    }
}
