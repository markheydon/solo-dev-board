namespace SoloDevBoard.App.Authentication;

/// <summary>Resolves GitHub authentication mode and identity details for user-facing pages.</summary>
public interface IGitHubAuthenticationSummaryService
{
    /// <summary>Returns the current GitHub authentication summary for display.</summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The authentication mode and GitHub identity summary.</returns>
    Task<GitHubAuthenticationSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
