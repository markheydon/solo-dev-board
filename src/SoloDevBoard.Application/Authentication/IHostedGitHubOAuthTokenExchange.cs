namespace SoloDevBoard.Application.Authentication;

/// <summary>Exchanges GitHub OAuth refresh tokens for access tokens (shared by web and worker).</summary>
public interface IHostedGitHubOAuthTokenExchange
{
    /// <summary>Uses a refresh token to obtain a new access token and optional rotated refresh token.</summary>
    Task<HostedGitHubRefreshTokenExchangeResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

/// <summary>Successful GitHub refresh token exchange.</summary>
public sealed record HostedGitHubRefreshTokenExchangeResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc,
    DateTimeOffset? AccessTokenExpiresAtUtc);
