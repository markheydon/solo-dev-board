namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Represents the GitHub connectivity state for PAT-only local trusted mode.</summary>
/// <param name="IsConnected">Whether the configured personal access token is connected to GitHub.</param>
/// <param name="OwnerLogin">The GitHub login when connected; otherwise <see langword="null"/>.</param>
/// <param name="StatusMessage">User-facing status summary.</param>
public sealed record GitHubConnectivityStatusDto(bool IsConnected, string? OwnerLogin, string StatusMessage);
