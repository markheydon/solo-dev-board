namespace SoloDevBoard.App.Authentication;

/// <summary>The rendered HTML and HTTP status for a static error page.</summary>
/// <param name="Html">The HTML body to return to the client.</param>
/// <param name="StatusCode">The HTTP status code associated with the failure.</param>
public sealed record StaticErrorPageResult(string Html, int StatusCode);
