using System.Diagnostics;
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
    };

    /// <summary>
    /// Query-string keys that must be redacted from inbound ASP.NET Core request URLs in telemetry.
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

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
            {
                return RedactAbsoluteUri(absoluteUri);
            }

            return url;
        }

        // Path-only URLs are not emitted by ASP.NET Core GetDisplayUrl(), but redact them defensively
        // in case custom enrichment passes a relative value.
        return RedactRelativeUrl(url);
    }

    /// <summary>
    /// Removes sensitive HTTP request header tags from a trace activity when present.
    /// </summary>
    /// <param name="activity">The activity to sanitise.</param>
    public static void StripSensitiveHeaderTags(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        foreach (var header in SensitiveRequestHeaders)
        {
            activity.SetTag($"http.request.header.{header.ToLowerInvariant()}", null);
        }
    }

    private static string RedactRelativeUrl(string url)
    {
        var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return url;
        }

        var path = url[..queryIndex];
        var redactedQuery = RedactQueryString(url[(queryIndex + 1)..]);
        return string.IsNullOrEmpty(redactedQuery) ? path : $"{path}?{redactedQuery}";
    }

    private static string RedactAbsoluteUri(Uri absoluteUri)
    {
        if (string.IsNullOrEmpty(absoluteUri.Query))
        {
            return absoluteUri.ToString();
        }

        var redactedQuery = RedactQueryString(absoluteUri.Query.TrimStart('?'));
        var builder = new UriBuilder(absoluteUri)
        {
            Query = redactedQuery,
        };

        return builder.Uri.ToString();
    }

    private static string RedactQueryString(string query)
    {
        var parsedQuery = QueryHelpers.ParseQuery(query);
        if (parsedQuery.Count == 0)
        {
            return string.Empty;
        }

        var redactedPairs = new List<string>(parsedQuery.Count);

        foreach (var pair in parsedQuery)
        {
            var value = SensitiveQueryKeys.Contains(pair.Key) ? "[Redacted]" : pair.Value.ToString();
            redactedPairs.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value)}");
        }

        return string.Join('&', redactedPairs);
    }
}
