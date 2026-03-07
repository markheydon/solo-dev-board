namespace SoloDevBoard.Application.Identity;

/// <summary>
/// Provides access to the current user's GitHub access token.
/// Introduced in Phase 2 (ADR-0007) so Application services remain decoupled
/// from concrete authentication configuration and can move to per-user context later.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Gets the GitHub access token for the current user context.</summary>
    /// <returns>The GitHub access token.</returns>
    string GetAccessToken();
}