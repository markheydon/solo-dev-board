namespace SoloDevBoard.Application.GitHub;

/// <summary>Parses GitHub repository full names in <c>owner/repository</c> form.</summary>
public static class RepositoryFullName
{
    /// <summary>Attempts to parse a repository full name into owner and repository segments.</summary>
    /// <param name="fullName">The repository identity in <c>owner/repository</c> form.</param>
    /// <param name="owner">When this method returns, contains the owner login if parsing succeeded; otherwise, an empty string.</param>
    /// <param name="repositoryName">When this method returns, contains the repository name if parsing succeeded; otherwise, an empty string.</param>
    /// <returns><see langword="true" /> if the value contains exactly two non-empty segments; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? fullName, out string owner, out string repositoryName)
    {
        owner = string.Empty;
        repositoryName = string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        var parts = SplitParts(fullName);
        if (parts.Length != 2)
        {
            return false;
        }

        owner = parts[0];
        repositoryName = parts[1];
        return true;
    }

    /// <summary>Resolves the owner login from a repository full name.</summary>
    /// <param name="fullName">The repository identity, typically in <c>owner/repository</c> form.</param>
    /// <returns>The owner segment, or an empty string when the value cannot be parsed.</returns>
    public static string ResolveOwner(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = SplitParts(fullName);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>Resolves the short repository name from a repository full name.</summary>
    /// <param name="fullName">The repository identity, typically in <c>owner/repository</c> form.</param>
    /// <returns>The repository segment, or an empty string when the value cannot be parsed.</returns>
    public static string ResolveRepositoryName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = SplitParts(fullName);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    /// <summary>Groups repository full names by owner, returning short repository names per owner.</summary>
    /// <param name="repositoryFullNames">The repository identities in <c>owner/repository</c> form.</param>
    /// <returns>A dictionary of owner login to distinct short repository names, ordered case-insensitively.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GroupByOwner(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        return repositoryFullNames
            .Select(fullName => new
            {
                Owner = ResolveOwner(fullName),
                Repository = ResolveRepositoryName(fullName),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Owner) && !string.IsNullOrWhiteSpace(item.Repository))
            .GroupBy(item => item.Owner, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(item => item.Repository)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Splits a repository full name into non-empty trimmed path segments.</summary>
    /// <param name="fullName">The repository identity to split.</param>
    /// <returns>The split path segments.</returns>
    private static string[] SplitParts(string fullName)
        => fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
