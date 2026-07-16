namespace SoloDevBoard.App.Authentication;

/// <summary>Initiates hosted authentication recovery when GitHub credentials are no longer valid.</summary>
public interface IHostedAuthenticationRecoveryService
{
    /// <summary>
    /// Attempts to recover from a hosted authentication failure by redirecting to the session-expired recovery route.
    /// </summary>
    /// <param name="exception">The exception raised by the failed operation.</param>
    /// <param name="returnUrl">The URL to return to after a successful re-sign-in.</param>
    /// <returns><see langword="true"/> when recovery was initiated; otherwise <see langword="false"/>.</returns>
    bool TryInitiateRecovery(Exception exception, string? returnUrl = null);
}
