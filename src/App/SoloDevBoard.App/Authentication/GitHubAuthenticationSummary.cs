namespace SoloDevBoard.App.Authentication;

/// <summary>GitHub authentication mode and identity details for display in the application shell.</summary>
/// <param name="ModeLabel">The active authentication mode label.</param>
/// <param name="IdentityLabel">The label describing the GitHub identity field.</param>
/// <param name="GitHubLogin">The GitHub login when known; otherwise <see langword="null"/>.</param>
public sealed record GitHubAuthenticationSummary(string ModeLabel, string IdentityLabel, string? GitHubLogin);
