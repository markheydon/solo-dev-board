namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Compares two board rules definitions and produces structured difference results.</summary>
public static class BoardRulesComparer
{
    /// <summary>Compares two board rules definitions and returns structured differences.</summary>
    /// <param name="left">The primary board rules definition.</param>
    /// <param name="right">The comparison board rules definition.</param>
    /// <returns>A comparison result describing missing, extra, and changed board structure.</returns>
    public static BoardRulesComparisonResultDto Compare(
        BoardRulesDefinitionDto left,
        BoardRulesDefinitionDto right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var differences = new List<BoardRulesComparisonDifferenceDto>();

        CompareColumns(left, right, differences);
        CompareRules(left, right, differences);
        CompareVisibility(left, right, differences);

        return new BoardRulesComparisonResultDto(left, right, differences);
    }

    private static void CompareColumns(
        BoardRulesDefinitionDto left,
        BoardRulesDefinitionDto right,
        ICollection<BoardRulesComparisonDifferenceDto> differences)
    {
        var leftColumnNames = left.Columns
            .Select(column => column.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        var rightColumnNames = right.Columns
            .Select(column => column.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        var leftColumnSet = new HashSet<string>(leftColumnNames, StringComparer.OrdinalIgnoreCase);
        var rightColumnSet = new HashSet<string>(rightColumnNames, StringComparer.OrdinalIgnoreCase);

        foreach (var columnName in leftColumnNames.Where(name => !rightColumnSet.Contains(name)))
        {
            differences.Add(new BoardRulesComparisonDifferenceDto(
                "Column",
                "Missing in comparison board",
                columnName,
                $"Column '{columnName}' exists on the primary board but not on the comparison board."));
        }

        foreach (var columnName in rightColumnNames.Where(name => !leftColumnSet.Contains(name)))
        {
            differences.Add(new BoardRulesComparisonDifferenceDto(
                "Column",
                "Extra on comparison board",
                columnName,
                $"Column '{columnName}' exists on the comparison board but not on the primary board."));
        }

        var sharedColumns = leftColumnNames
            .Where(name => rightColumnSet.Contains(name))
            .ToArray();

        if (sharedColumns.Length > 1)
        {
            var leftOrder = sharedColumns
                .Select(name => Array.FindIndex(leftColumnNames, candidate => candidate.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            var rightOrder = sharedColumns
                .Select(name => Array.FindIndex(rightColumnNames, candidate => candidate.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (!leftOrder.SequenceEqual(rightOrder))
            {
                differences.Add(new BoardRulesComparisonDifferenceDto(
                    "Column",
                    "Order changed",
                    "Shared columns",
                    $"Shared columns appear in a different order. Primary: {string.Join(" → ", leftColumnNames)}. Comparison: {string.Join(" → ", rightColumnNames)}."));
            }
        }
    }

    private static void CompareRules(
        BoardRulesDefinitionDto left,
        BoardRulesDefinitionDto right,
        ICollection<BoardRulesComparisonDifferenceDto> differences)
    {
        var leftRules = left.Rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Name))
            .GroupBy(rule => rule.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var rightRules = right.Rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Name))
            .GroupBy(rule => rule.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var (ruleName, leftRule) in leftRules.Where(pair => !rightRules.ContainsKey(pair.Key)))
        {
            differences.Add(new BoardRulesComparisonDifferenceDto(
                "Rule",
                "Missing in comparison board",
                ruleName,
                $"Rule '{ruleName}' exists on the primary board but not on the comparison board."));
        }

        foreach (var (ruleName, rightRule) in rightRules.Where(pair => !leftRules.ContainsKey(pair.Key)))
        {
            differences.Add(new BoardRulesComparisonDifferenceDto(
                "Rule",
                "Extra on comparison board",
                ruleName,
                $"Rule '{ruleName}' exists on the comparison board but not on the primary board."));
        }

        foreach (var ruleName in leftRules.Keys.Where(rightRules.ContainsKey))
        {
            var leftRule = leftRules[ruleName];
            var rightRule = rightRules[ruleName];

            if (!RulesAreEquivalent(leftRule, rightRule))
            {
                differences.Add(new BoardRulesComparisonDifferenceDto(
                    "Rule",
                    "Changed",
                    ruleName,
                    BuildRuleChangeDetails(leftRule, rightRule)));
            }
        }
    }

    private static void CompareVisibility(
        BoardRulesDefinitionDto left,
        BoardRulesDefinitionDto right,
        ICollection<BoardRulesComparisonDifferenceDto> differences)
    {
        if (left.HasFullVisibility == right.HasFullVisibility)
        {
            return;
        }

        differences.Add(new BoardRulesComparisonDifferenceDto(
            "Visibility",
            "Changed",
            "Board metadata visibility",
            left.HasFullVisibility
                ? "The primary board has full visibility, but the comparison board is only partially visible."
                : "The comparison board has full visibility, but the primary board is only partially visible."));
    }

    private static bool RulesAreEquivalent(BoardRuleDto leftRule, BoardRuleDto rightRule)
        => leftRule.IsEnabled == rightRule.IsEnabled
            && string.Equals(leftRule.Trigger.Trim(), rightRule.Trigger.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(leftRule.Action.Trim(), rightRule.Action.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string BuildRuleChangeDetails(BoardRuleDto leftRule, BoardRuleDto rightRule)
    {
        var changes = new List<string>();

        if (!string.Equals(leftRule.Trigger.Trim(), rightRule.Trigger.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"trigger changed from '{leftRule.Trigger}' to '{rightRule.Trigger}'");
        }

        if (!string.Equals(leftRule.Action.Trim(), rightRule.Action.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"action changed from '{leftRule.Action}' to '{rightRule.Action}'");
        }

        if (leftRule.IsEnabled != rightRule.IsEnabled)
        {
            changes.Add($"enabled state changed from {(leftRule.IsEnabled ? "enabled" : "disabled")} to {(rightRule.IsEnabled ? "enabled" : "disabled")}");
        }

        return $"Rule '{leftRule.Name}' differs: {string.Join("; ", changes)}.";
    }
}
