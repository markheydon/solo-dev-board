using System.Globalization;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Formats build timestamps for display on the About page.</summary>
internal static class AppVersionBuiltAtFormatter
{
    private static readonly TimeZoneInfo LondonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    private static readonly CultureInfo UnitedKingdomCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>
    /// Formats a UTC build timestamp for pre-release versions.
    /// Returns an empty string for release versions or when no timestamp is available.
    /// </summary>
    /// <param name="version">The application version string.</param>
    /// <param name="buildTimestampUtc">The UTC build timestamp, when stamped at compile time.</param>
    /// <returns>A UK-localised display string, or empty when not applicable.</returns>
    public static string FormatDisplay(string version, DateTimeOffset? buildTimestampUtc)
    {
        if (string.IsNullOrWhiteSpace(version) || !version.Contains('-', StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (buildTimestampUtc is null)
        {
            return string.Empty;
        }

        var utc = buildTimestampUtc.Value.UtcDateTime;
        var london = TimeZoneInfo.ConvertTimeFromUtc(utc, LondonTimeZone);
        var timeZoneAbbreviation = LondonTimeZone.IsDaylightSavingTime(london) ? "BST" : "GMT";
        var formatted = london.ToString("d MMM yy @ HH:mm", UnitedKingdomCulture);
        return $"{formatted} {timeZoneAbbreviation}";
    }
}
