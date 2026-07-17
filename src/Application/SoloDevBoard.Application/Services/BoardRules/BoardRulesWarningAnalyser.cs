namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Analyses board rules for potential conflicts and incomplete configuration.</summary>
public static class BoardRulesWarningAnalyser
{
    /// <summary>Analyses the supplied board rules definition for warning conditions.</summary>
    /// <param name="definition">The board rules definition to analyse.</param>
    /// <returns>Human-readable warning messages for detected issues.</returns>
    public static IReadOnlyList<string> AnalyseWarnings(BoardRulesDefinitionDto? definition)
    {
        if (definition?.Rules is not { Count: > 0 } rules)
        {
            return [];
        }

        var duplicateTriggerWarnings = rules
            .GroupBy(
                rule => string.IsNullOrWhiteSpace(rule.Trigger) ? string.Empty : rule.Trigger.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group =>
                $"Rules with the same trigger '{group.Key}' may conflict: {string.Join(", ", group.Select(rule => rule.Name))}.")
            .ToArray();

        var incompleteConfigurationWarnings = rules
            .Where(rule => string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Action))
            .Select(rule => $"Rule '{rule.Name}' has incomplete configuration and may behave unexpectedly.")
            .ToArray();

        return duplicateTriggerWarnings.Concat(incompleteConfigurationWarnings).ToArray();
    }

    /// <summary>Determines whether the supplied rule is referenced by a warning message.</summary>
    /// <param name="rule">The rule to inspect.</param>
    /// <param name="warnings">The warning messages produced by <see cref="AnalyseWarnings"/>.</param>
    /// <returns><see langword="true"/> when the rule appears in a warning message; otherwise <see langword="false"/>.</returns>
    public static bool IsRuleWarning(BoardRuleDto rule, IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(warnings);

        return warnings.Any(warning => warning.Contains($"'{rule.Name}'", StringComparison.OrdinalIgnoreCase));
    }
}
