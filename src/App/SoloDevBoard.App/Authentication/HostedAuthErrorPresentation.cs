using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Represents user-facing copy for a hosted authentication error.</summary>
/// <param name="Title">The page title and heading.</param>
/// <param name="Message">The explanatory message shown to the user.</param>
/// <param name="StatusCode">The HTTP status code associated with the failure.</param>
public readonly record struct HostedAuthErrorPresentation(string Title, string Message, int StatusCode);

/// <summary>Maps hosted authentication error reason codes to user-facing presentation.</summary>
public static class HostedAuthErrorPresentationMapper
{
    private static readonly IReadOnlyDictionary<string, HostedAuthErrorPresentation> Presentations =
        new Dictionary<string, HostedAuthErrorPresentation>(StringComparer.Ordinal)
        {
            [HostedAuthErrorRoutes.AccessDenied] = new(
                "Access denied",
                "You signed in successfully, but your GitHub account is not authorised for this deployment. Contact the operator if you believe this is a mistake.",
                StatusCodes.Status403Forbidden),
            [HostedAuthErrorRoutes.SignInDenied] = new(
                "Sign-in cancelled",
                "GitHub sign-in was cancelled or denied. Try again if you still need access.",
                StatusCodes.Status401Unauthorized),
            [HostedAuthErrorRoutes.SignInStateInvalid] = new(
                "Sign-in session expired",
                "Your sign-in session expired or was invalid. Start the sign-in flow again.",
                StatusCodes.Status401Unauthorized),
            [HostedAuthErrorRoutes.SignInIncomplete] = new(
                "Sign-in incomplete",
                "GitHub sign-in did not complete. Start the sign-in flow again.",
                StatusCodes.Status400BadRequest),
            [HostedAuthErrorRoutes.SignInFailed] = new(
                "Sign-in failed",
                "GitHub sign-in could not be completed. Check that the GitHub App is installed for your account and try again.",
                StatusCodes.Status401Unauthorized),
            [HostedAuthErrorRoutes.SignInUnavailable] = new(
                "GitHub unavailable",
                "GitHub sign-in could not complete because GitHub returned an unexpected response. Try again later.",
                StatusCodes.Status502BadGateway),
            [HostedAuthErrorRoutes.SignInUnknown] = new(
                "Sign-in failed",
                "Hosted sign-in could not be completed. Try again, or contact the operator if the problem continues.",
                StatusCodes.Status401Unauthorized),
            [HostedAuthErrorRoutes.SessionExpired] = new(
                "Session expired",
                "Your GitHub sign-in is no longer valid. This can happen when your token expires or access is revoked. Sign in again to continue.",
                StatusCodes.Status401Unauthorized),
        };

    /// <summary>Resolves user-facing presentation for a hosted authentication failure reason.</summary>
    /// <param name="reason">The reason code supplied on the error page query string.</param>
    /// <returns>The presentation to show to the user.</returns>
    public static HostedAuthErrorPresentation Resolve(string? reason)
    {
        if (!string.IsNullOrWhiteSpace(reason)
            && Presentations.TryGetValue(reason, out var presentation))
        {
            return presentation;
        }

        return Presentations[HostedAuthErrorRoutes.SignInUnknown];
    }
}
