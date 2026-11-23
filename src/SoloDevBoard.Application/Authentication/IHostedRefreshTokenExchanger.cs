namespace SoloDevBoard.Application.Authentication;

/// <summary>Exchanges a stored refresh token for a new access token (hosted sign-in only).</summary>
public interface IHostedRefreshTokenExchanger
{
    /// <summary>Refreshes GitHub tokens.</summary>
    Task<HostedRefreshTokenExchangeResult> ExchangeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a refresh-token exchange.</summary>
/// <param name="Succeeded">Whether GitHub returned an access token.</param>
/// <param name="AccessToken">Access token when succeeded.</param>
/// <param name="RefreshToken">Rotated refresh token.</param>
/// <param name="RefreshTokenExpiresAtUtc">Refresh token expiry when known.</param>
public sealed record HostedRefreshTokenExchangeResult(
    bool Succeeded,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc);
