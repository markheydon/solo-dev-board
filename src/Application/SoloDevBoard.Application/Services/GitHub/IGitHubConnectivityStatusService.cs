namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Provides GitHub connectivity status for PAT-only local trusted mode.</summary>
public interface IGitHubConnectivityStatusService
{
    /// <summary>Returns the current GitHub connectivity status for display in the application shell.</summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The connectivity status for PAT mode, or a not-applicable state when hosted sign-in is enabled.</returns>
    Task<GitHubConnectivityStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
