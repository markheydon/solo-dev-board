using System.Globalization;
using System.Security.Claims;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Validates hosted GitHub token expiry claims on authenticated principals.</summary>
public static class HostedTokenExpiryValidator
{
    /// <summary>Determines whether the hosted GitHub token on the principal has expired.</summary>
    /// <param name="principal">The authenticated principal to inspect.</param>
    /// <param name="authOptions">GitHub authentication options.</param>
    /// <returns><see langword="true"/> when the token expiry claim is present and in the past; otherwise <see langword="false"/>.</returns>
    public static bool IsExpired(ClaimsPrincipal? principal, GitHubAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        var expiryClaimType = authOptions.HostedTokenExpiresAtClaimType;
        if (string.IsNullOrWhiteSpace(expiryClaimType))
        {
            return false;
        }

        var expiryClaim = principal?.FindFirst(expiryClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(expiryClaim))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                expiryClaim,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiresAtUtc))
        {
            return false;
        }

        return expiresAtUtc <= DateTimeOffset.UtcNow;
    }
}
