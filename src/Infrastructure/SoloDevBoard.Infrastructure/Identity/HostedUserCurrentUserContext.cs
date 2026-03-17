using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from hosted per-request authentication claims.
/// </summary>
public sealed class HostedUserCurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    IOptions<GitHubAuthOptions> authOptions) : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The hosted request does not contain a GitHub owner login claim.</exception>
    public string OwnerLogin
    {
        get
        {
            var ownerLogin = GetRequiredClaimValue(_authOptions.HostedOwnerLoginClaimType, "GitHub owner login");

            return ownerLogin;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The hosted request does not contain an access-token claim, or the hosted token is expired.
    /// </exception>
    public string GetAccessToken()
    {
        var accessToken = GetRequiredClaimValue(_authOptions.HostedAccessTokenClaimType, "GitHub access token");

        EnsureTokenNotExpired();

        return accessToken;
    }

    private string GetRequiredClaimValue(string claimType, string claimDescription)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            throw new InvalidOperationException("Hosted authentication claim mapping is invalid because a required claim type is empty.");
        }

        var claimValue = _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            throw new InvalidOperationException(
                $"Hosted authentication is enabled but the {claimDescription} claim ('{claimType}') is missing.");
        }

        return claimValue;
    }

    private void EnsureTokenNotExpired()
    {
        var expiryClaimType = _authOptions.HostedTokenExpiresAtClaimType;
        if (string.IsNullOrWhiteSpace(expiryClaimType))
        {
            return;
        }

        var expiryClaim = _httpContextAccessor.HttpContext?.User.FindFirst(expiryClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(expiryClaim))
        {
            return;
        }

        if (!DateTimeOffset.TryParse(expiryClaim, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiresAtUtc))
        {
            throw new InvalidOperationException(
                $"Hosted authentication token expiry claim ('{expiryClaimType}') is not a valid UTC timestamp.");
        }

        if (expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Hosted GitHub access token has expired and must be refreshed by signing in again.");
        }
    }
}
