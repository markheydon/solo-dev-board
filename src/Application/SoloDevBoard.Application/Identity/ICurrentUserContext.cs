namespace SoloDevBoard.Application.Identity;

/// <summary>
/// Provides access to the current user's GitHub owner login and access token.
/// Introduced in Phase 2 (ADR-0007) so Application services remain decoupled
/// from concrete authentication configuration and can move to per-user context later.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Gets the GitHub owner login for the current user context.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no owner login is available for the current user context.</exception>
    string OwnerLogin { get; }

    /// <summary>Gets the GitHub access token for the current user context.</summary>
    /// <returns>A non-empty GitHub access token for the current user context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no access token is available for the current user context.</exception>
    string GetAccessToken();
}