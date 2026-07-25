using Microsoft.AspNetCore.WebUtilities;

namespace SoloDevBoard.App.Authentication;

/// <summary>Helpers for preserving return URLs through the hosted sign-in landing page.</summary>
public static class HostedAuthReturnUrl
{
    /// <summary>Gets a safe return URL from the current request query string.</summary>
    /// <param name="requestUri">The current request URI.</param>
    /// <returns>A safe relative return URL when present; otherwise <see langword="null"/>.</returns>
    public static string? GetRequestedReturnUrl(Uri requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        var query = QueryHelpers.ParseQuery(requestUri.Query);

        if (query.TryGetValue("returnUrl", out var returnUrlValues))
        {
            return GetSafeReturnUrl(returnUrlValues.FirstOrDefault());
        }

        if (query.TryGetValue("ReturnUrl", out var capitalisedReturnUrlValues))
        {
            return GetSafeReturnUrl(capitalisedReturnUrlValues.FirstOrDefault());
        }

        return null;
    }

    /// <summary>Builds the hosted sign-in route, preserving an optional return URL.</summary>
    /// <param name="returnUrl">The return URL to preserve after sign-in.</param>
    /// <returns>The sign-in route.</returns>
    public static string BuildSignInUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/auth/sign-in";
        }

        return QueryHelpers.AddQueryString("/auth/sign-in", "returnUrl", returnUrl);
    }

    /// <summary>Resolves the post-sign-in destination.</summary>
    /// <param name="returnUrl">An optional return URL.</param>
    /// <returns>The destination route.</returns>
    public static string ResolveDestination(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

    private static string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        return returnUrl;
    }
}
