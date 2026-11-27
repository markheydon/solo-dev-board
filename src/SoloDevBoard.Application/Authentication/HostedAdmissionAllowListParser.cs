namespace SoloDevBoard.Application.Authentication;

/// <summary>Parses hosted admission allow-list configuration for Application rules.</summary>
public static class HostedAdmissionAllowListParser
{
    private static readonly char[] EntrySeparators = [',', ';', ' '];

    /// <summary>Returns active allow-list entries as a normalised set.</summary>
    public static HashSet<string> BuildNormalisedSet(string? value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var segment in value.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment.Length == 0 || segment.StartsWith('$'))
            {
                continue;
            }

            result.Add(segment);
        }

        return result;
    }
}
