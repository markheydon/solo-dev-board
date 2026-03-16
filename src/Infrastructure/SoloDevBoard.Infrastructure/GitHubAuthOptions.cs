using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure;

/// <summary>Configuration options for GitHub authentication.</summary>
public sealed class GitHubAuthOptions
{
    public const string SectionName = "GitHubAuth";

    /// <summary>GitHub owner login used for owner-scoped repository operations.</summary>
    public string OwnerLogin { get; set; } = string.Empty;

    /// <summary>Personal access token for GitHub API authentication.</summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>GitHub App ID, used when authenticating as a GitHub App.</summary>
    public string GitHubAppId { get; set; } = string.Empty;

    /// <summary>GitHub App private key in PEM format.</summary>
    public string GitHubAppPrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value that indicates whether hosted sign-in mode is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to resolve the current user from per-request hosted authentication claims; otherwise, <see langword="false" />.
    /// The default is <see langword="false" />.
    /// </value>
    public bool HostedSignInEnabled { get; set; }

    /// <summary>
    /// Gets or sets the claim type used to read the authenticated GitHub owner login in hosted mode.
    /// </summary>
    /// <value>The hosted authentication claim type for owner login.</value>
    public string HostedOwnerLoginClaimType { get; set; } = HostedAuthClaimTypes.OwnerLogin;

    /// <summary>
    /// Gets or sets the claim type used to read the hosted GitHub access token.
    /// </summary>
    /// <value>The hosted authentication claim type for the access token.</value>
    public string HostedAccessTokenClaimType { get; set; } = HostedAuthClaimTypes.AccessToken;

    /// <summary>
    /// Gets or sets the claim type used to read the hosted GitHub installation identifier.
    /// </summary>
    /// <value>The hosted authentication claim type for the installation identifier.</value>
    public string HostedInstallationIdClaimType { get; set; } = HostedAuthClaimTypes.InstallationId;

    /// <summary>
    /// Gets or sets the claim type used to read the hosted GitHub token expiry timestamp.
    /// </summary>
    /// <value>The hosted authentication claim type for token expiry in UTC.</value>
    public string HostedTokenExpiresAtClaimType { get; set; } = HostedAuthClaimTypes.TokenExpiresAt;

    /// <summary>
    /// Gets or sets a value indicating whether the legacy OAuth App hosted sign-in fallback boundary is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to expose the fallback-only compatibility boundary; otherwise, <see langword="false" />.
    /// The default is <see langword="false" /> to keep GitHub App-first hosted sign-in as the standard path.
    /// </value>
    public bool HostedOAuthAppFallbackEnabled { get; set; }

    /// <summary>
    /// Gets or sets the GitHub App client identifier used for hosted user sign-in.
    /// </summary>
    /// <value>The hosted GitHub App client identifier.</value>
    public string HostedGitHubAppClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub App client secret used for hosted user sign-in.
    /// </summary>
    /// <value>The hosted GitHub App client secret.</value>
    public string HostedGitHubAppClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback path used by the hosted sign-in handshake.
    /// </summary>
    /// <value>The callback path for hosted sign-in.</value>
    public string HostedSignInCallbackPath { get; set; } = "/auth/callback";

    /// <summary>
    /// Gets or sets an optional absolute callback base URI used for hosted sign-in.
    /// </summary>
    /// <value>
    /// The absolute callback base URI, for example <c>https://localhost:7179</c>.
    /// When empty, the current request scheme and host are used.
    /// </value>
    public string HostedSignInCallbackBaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub authorisation endpoint used for hosted sign-in.
    /// </summary>
    /// <value>The hosted GitHub authorisation endpoint.</value>
    public string HostedGitHubAuthoriseEndpoint { get; set; } = "https://github.com/login/oauth/authorize";

    /// <summary>
    /// Gets or sets the GitHub access-token endpoint used for hosted sign-in.
    /// </summary>
    /// <value>The hosted GitHub access-token endpoint.</value>
    public string HostedGitHubAccessTokenEndpoint { get; set; } = "https://github.com/login/oauth/access_token";

    /// <summary>
    /// Gets or sets the scopes requested during hosted sign-in.
    /// </summary>
    /// <value>The hosted sign-in scopes as a space-separated string.</value>
    public string HostedSignInScopes { get; set; } = "read:user read:org";
}
