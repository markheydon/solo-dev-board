namespace SoloDevBoard.App.Authentication;

/// <summary>Initiates GitHub authentication recovery for hosted sign-in and PAT connectivity failures.</summary>
public interface IGitHubAuthenticationRecoveryService
{
    /// <summary>
    /// Attempts to recover from a GitHub authentication failure by redirecting to the appropriate recovery route.
    /// </summary>
    /// <param name="exception">The exception raised by the failed operation.</param>
    /// <param name="returnUrl">The URL to return to after recovery.</param>
    /// <returns><see langword="true"/> when recovery was initiated; otherwise <see langword="false"/>.</returns>
    bool TryInitiateRecovery(Exception exception, string? returnUrl = null);
}
