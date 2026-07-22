using Microsoft.AspNetCore.WebUtilities;

namespace SoloDevBoard.ServiceDefaults.Telemetry;

/// <summary>
/// Redacts sensitive values from telemetry and log enrichment payloads.
/// </summary>
public static class TelemetryRedaction
{
    /// <summary>
    /// HTTP request headers that must never be copied into telemetry attributes.
    /// </summary>
    public static IReadOnlySet<string> SensitiveRequestHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-GitHub-Api-Version",
    };

    /// <summary>
    /// Query-string keys that must be redacted from request URLs in telemetry.
    /// </summary>
    public static IReadOnlySet<string> SensitiveQueryKeys { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "code",
        "state",
        "token",
        "access_token",
        "refresh_token",
        "client_secret",
    };

    /// <summary>
    /// Redacts sensitive query-string values from an HTTP URL before it is attached to telemetry.
    /// </summary>
    /// <param name="url">The URL to redact.</param>
    /// <returns>A URL with sensitive query values replaced by a redaction marker.</returns>
    public static string RedactHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return url;
        }

        if (string.IsNullOrEmpty(absoluteUri.Query))
        {
            return url;
        }

        var query = QueryHelpers.ParseQuery(absoluteUri.Query);
        var redactedPairs = new List<string>(query.Count);

        foreach (var pair in query)
        {
            var value = SensitiveQueryKeys.Contains(pair.Key) ? "[Redacted]" : pair.Value.ToString();
            redactedPairs.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value)}");
        }

        var builder = new UriBuilder(absoluteUri)
        {
            Query = string.Join('&', redactedPairs),
        };

        return builder.Uri.ToString();
    }
}
