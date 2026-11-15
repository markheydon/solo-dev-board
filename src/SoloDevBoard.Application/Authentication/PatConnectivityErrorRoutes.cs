namespace SoloDevBoard.Application.Authentication;

/// <summary>Defines PAT connectivity error page routes and reason codes.</summary>
public static class PatConnectivityErrorRoutes
{
    /// <summary>Gets the PAT connectivity error page path.</summary>
    public const string ErrorPath = "/auth/connectivity-error";

    /// <summary>Gets the reason code when GitHub rejects the configured personal access token.</summary>
    public const string TokenRejected = "token-rejected";

    /// <summary>Gets the fallback reason code for unknown PAT connectivity failures.</summary>
    public const string Unknown = "connectivity-unknown";

    /// <summary>Builds the PAT connectivity error page URL for the supplied reason code.</summary>
    /// <param name="reason">The PAT connectivity failure reason code.</param>
    /// <param name="returnUrl">An optional return URL to preserve after recovery.</param>
    /// <returns>The error page URL including the reason query parameter.</returns>
    public static string BuildErrorUrl(string reason, string? returnUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var url = $"{ErrorPath}?reason={Uri.EscapeDataString(reason)}";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            url += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        return url;
    }
}
