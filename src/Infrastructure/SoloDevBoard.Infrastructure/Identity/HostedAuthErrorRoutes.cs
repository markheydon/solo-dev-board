using Microsoft.AspNetCore.WebUtilities;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Defines hosted authentication error page routes and reason codes.</summary>
public static class HostedAuthErrorRoutes
{
    /// <summary>Gets the hosted authentication error page path.</summary>
    public const string ErrorPath = "/auth/error";

    /// <summary>Gets the reason code when hosted admission denies access.</summary>
    public const string AccessDenied = "access-denied";

    /// <summary>Gets the reason code when GitHub denies or cancels sign-in.</summary>
    public const string SignInDenied = "sign-in-denied";

    /// <summary>Gets the reason code when hosted sign-in state is missing or invalid.</summary>
    public const string SignInStateInvalid = "sign-in-state-invalid";

    /// <summary>Gets the reason code when the GitHub callback omits an authorisation code.</summary>
    public const string SignInIncomplete = "sign-in-incomplete";

    /// <summary>Gets the reason code when hosted sign-in cannot establish a session.</summary>
    public const string SignInFailed = "sign-in-failed";

    /// <summary>Gets the reason code when GitHub is unavailable during sign-in.</summary>
    public const string SignInUnavailable = "sign-in-unavailable";

    /// <summary>Gets the reason code when a requested sign-in method is not configured.</summary>
    public const string SignInMisconfigured = "sign-in-misconfigured";

    /// <summary>Gets the fallback reason code for unknown hosted authentication failures.</summary>
    public const string SignInUnknown = "sign-in-unknown";

    /// <summary>Gets the reason code when a hosted session is no longer valid and requires re-sign-in.</summary>
    public const string SessionExpired = "session-expired";

    /// <summary>Builds the hosted authentication error page URL for the supplied reason code.</summary>
    /// <param name="reason">The hosted authentication failure reason code.</param>
    /// <param name="returnUrl">An optional return URL to preserve after re-sign-in.</param>
    /// <returns>The error page URL including the reason query parameter.</returns>
    public static string BuildErrorUrl(string reason, string? returnUrl = null)
    {
        var url = QueryHelpers.AddQueryString(ErrorPath, "reason", reason);

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        }

        return url;
    }
}
