using System.Text.RegularExpressions;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Sanitises GitHub logins for snapshot storage keys.</summary>
public static partial class OperatorLoginSanitiser
{
    /// <summary>Returns the sanitised login, or null when the login is unsafe.</summary>
    public static string? Sanitise(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        var trimmed = login.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal) ||
            trimmed.Contains('/', StringComparison.Ordinal) ||
            trimmed.Contains('\\', StringComparison.Ordinal))
        {
            return null;
        }

        return GitHubLoginPattern().IsMatch(trimmed) ? trimmed : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9-]{0,37}[A-Za-z0-9])?$")]
    private static partial Regex GitHubLoginPattern();
}
