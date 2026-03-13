namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Defines hosted authentication claim types used for per-request GitHub user context resolution.
/// </summary>
public static class HostedAuthClaimTypes
{
    /// <summary>Gets the claim type carrying the authenticated GitHub owner login.</summary>
    public const string OwnerLogin = "solo-dev-board.github.owner-login";

    /// <summary>Gets the claim type carrying the hosted GitHub access token.</summary>
    public const string AccessToken = "solo-dev-board.github.access-token";

    /// <summary>Gets the claim type carrying the hosted GitHub installation identifier.</summary>
    public const string InstallationId = "solo-dev-board.github.installation-id";

    /// <summary>Gets the claim type carrying the hosted GitHub access token expiry timestamp (UTC).</summary>
    public const string TokenExpiresAt = "solo-dev-board.github.token-expires-at";
}
