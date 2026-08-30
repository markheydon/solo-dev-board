using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Parses hosted admission allow-list configuration values.</summary>
public static class HostedAdmissionAllowList
{
    private static readonly char[] EntrySeparators = [',', ';', ' '];

    /// <summary>Returns <see langword="true" /> when the allow-list has at least one active entry.</summary>
    public static bool HasConfiguredEntries(string? value)
    {
        foreach (var entry in EnumerateEntries(value))
        {
            if (AuthConfigurationPlaceholders.IsConfigured(entry))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns a normalised set of active allow-list entries.</summary>
    public static HashSet<string> BuildNormalisedSet(string? value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in EnumerateEntries(value))
        {
            if (AuthConfigurationPlaceholders.IsConfigured(entry))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static IEnumerable<string> EnumerateEntries(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var entry in value.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return entry;
        }
    }
}
