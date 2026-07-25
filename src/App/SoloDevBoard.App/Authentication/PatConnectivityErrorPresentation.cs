using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Represents user-facing copy for a PAT connectivity error.</summary>
/// <param name="Title">The page title and heading.</param>
/// <param name="Message">The explanatory message shown to the user.</param>
/// <param name="StatusCode">The HTTP status code associated with the failure.</param>
public readonly record struct PatConnectivityErrorPresentation(string Title, string Message, int StatusCode);

/// <summary>Maps PAT connectivity error reason codes to user-facing presentation.</summary>
public static class PatConnectivityErrorPresentationMapper
{
    private static readonly IReadOnlyDictionary<string, PatConnectivityErrorPresentation> Presentations =
        new Dictionary<string, PatConnectivityErrorPresentation>(StringComparer.Ordinal)
        {
            [PatConnectivityErrorRoutes.TokenRejected] = new(
                "GitHub connection problem",
                "SoloDevBoard could not authenticate with GitHub using the configured personal access token. " +
                "This is a PAT configuration problem, not a hosted sign-in session problem. " +
                "Update the token via Aspire parameters or user secrets, then restart the application.",
                StatusCodes.Status401Unauthorized),
            [PatConnectivityErrorRoutes.Unknown] = new(
                "GitHub connection problem",
                "SoloDevBoard could not verify GitHub connectivity using the configured personal access token. " +
                "Check your PAT configuration and restart the application.",
                StatusCodes.Status503ServiceUnavailable),
        };

    /// <summary>Resolves user-facing presentation for a PAT connectivity failure reason.</summary>
    /// <param name="reason">The reason code supplied on the error page query string.</param>
    /// <returns>The presentation to show to the user.</returns>
    public static PatConnectivityErrorPresentation Resolve(string? reason)
    {
        if (!string.IsNullOrWhiteSpace(reason)
            && Presentations.TryGetValue(reason, out var presentation))
        {
            return presentation;
        }

        return Presentations[PatConnectivityErrorRoutes.Unknown];
    }
}
