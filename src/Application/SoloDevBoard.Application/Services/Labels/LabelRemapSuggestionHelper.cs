namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Suggests destination labels when remapping extras onto a recommended taxonomy.</summary>
public static class LabelRemapSuggestionHelper
{
    private static readonly HashSet<string> AmbiguousGitHubDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        "enhancement",
        "question",
        "wontfix",
    };

    private static readonly Dictionary<string, string> GitHubDefaultCounterparts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bug"] = "type/bug",
        ["documentation"] = "type/documentation",
    };

    private static readonly HashSet<string> WorkflowPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "priority",
        "status",
        "size",
    };

    /// <summary>
    /// Suggests a destination label for a source extra, or returns <see langword="null" /> when the user should choose or skip.
    /// </summary>
    /// <param name="sourceLabelName">The extra label name to remap.</param>
    /// <param name="strategyLabelNames">The label names in the selected recommended strategy.</param>
    /// <returns>A suggested destination name when a clear match exists; otherwise, <see langword="null" />.</returns>
    public static string? SuggestDestination(string sourceLabelName, IReadOnlyList<string> strategyLabelNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabelName);
        ArgumentNullException.ThrowIfNull(strategyLabelNames);

        if (AmbiguousGitHubDefaults.Contains(sourceLabelName))
        {
            return null;
        }

        if (GitHubDefaultCounterparts.TryGetValue(sourceLabelName, out var counterpart)
            && strategyLabelNames.Any(name => string.Equals(name, counterpart, StringComparison.OrdinalIgnoreCase)))
        {
            return counterpart;
        }

        foreach (var strategyName in strategyLabelNames)
        {
            var slashIndex = strategyName.LastIndexOf('/');
            if (slashIndex <= 0 || slashIndex >= strategyName.Length - 1)
            {
                continue;
            }

            var prefix = strategyName[..slashIndex];
            if (!WorkflowPrefixes.Contains(prefix))
            {
                continue;
            }

            var leaf = strategyName[(slashIndex + 1)..];
            if (string.Equals(leaf, sourceLabelName, StringComparison.OrdinalIgnoreCase))
            {
                return strategyName;
            }
        }

        return null;
    }

    /// <summary>Builds the destination options for a remap row.</summary>
    /// <param name="sourceLabelName">The source label being remapped.</param>
    /// <param name="strategyLabelNames">The label names in the selected recommended strategy.</param>
    /// <param name="existingLabelNames">Label names that will remain in the repository after apply (create, update, or skip).</param>
    /// <param name="excludedLabelNames">Label names that must not be offered as destinations, such as extras slated for deletion.</param>
    /// <returns>A sorted, distinct list of destination candidates excluding the source and excluded names.</returns>
    public static IReadOnlyList<string> BuildDestinationOptions(
        string sourceLabelName,
        IReadOnlyList<string> strategyLabelNames,
        IReadOnlyList<string> existingLabelNames,
        IReadOnlyList<string>? excludedLabelNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabelName);
        ArgumentNullException.ThrowIfNull(strategyLabelNames);
        ArgumentNullException.ThrowIfNull(existingLabelNames);

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourceLabelName };
        if (excludedLabelNames is not null)
        {
            foreach (var name in excludedLabelNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    excluded.Add(name);
                }
            }
        }

        return strategyLabelNames
            .Concat(existingLabelNames)
            .Where(name => !string.IsNullOrWhiteSpace(name) && !excluded.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
