using SoloDevBoard.Application.Services.BoardRules;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BoardRulesWarningAnalyser"/>.</summary>
public sealed class BoardRulesWarningAnalyserTests
{
    [Fact]
    public void AnalyseWarnings_NullDefinition_ReturnsEmptyWarnings()
    {
        // Act
        var result = BoardRulesWarningAnalyser.AnalyseWarnings(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyseWarnings_EmptyRules_ReturnsEmptyWarnings()
    {
        // Arrange
        var definition = CreateDefinition(rules: []);

        // Act
        var result = BoardRulesWarningAnalyser.AnalyseWarnings(definition);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyseWarnings_DuplicateTriggers_ReturnsConflictWarning()
    {
        // Arrange
        var definition = CreateDefinition(
        [
            new BoardRuleDto(1, "Rule A", "When item added", "Assign reviewer", true),
            new BoardRuleDto(2, "Rule B", "When item added", "Close issue", true),
        ]);

        // Act
        var result = BoardRulesWarningAnalyser.AnalyseWarnings(definition);

        // Assert
        Assert.Single(result);
        Assert.Contains("Rules with the same trigger 'When item added' may conflict", result[0], StringComparison.Ordinal);
        Assert.Contains("Rule A", result[0], StringComparison.Ordinal);
        Assert.Contains("Rule B", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public void AnalyseWarnings_IncompleteConfiguration_ReturnsIncompleteWarning()
    {
        // Arrange
        var definition = CreateDefinition(
        [
            new BoardRuleDto(1, "Incomplete rule", "When item added", string.Empty, true),
        ]);

        // Act
        var result = BoardRulesWarningAnalyser.AnalyseWarnings(definition);

        // Assert
        Assert.Single(result);
        Assert.Contains("Rule 'Incomplete rule' has incomplete configuration", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public void AnalyseWarnings_UnsupportedPartialData_ReturnsWarningsForAvailableRulesOnly()
    {
        // Arrange
        var definition = new BoardRulesDefinitionDto(
            "PVT_alpha",
            "Alpha Board",
            "owner",
            [],
            [
                new BoardRuleDto(1, "Duplicate A", "When stale", "Close issue", true),
                new BoardRuleDto(2, "Duplicate B", "When stale", "Archive issue", true),
            ],
            ["Board automation rules are not yet available through the current GitHub query model."]);

        // Act
        var result = BoardRulesWarningAnalyser.AnalyseWarnings(definition);

        // Assert
        Assert.Single(result);
        Assert.Contains("When stale", result[0], StringComparison.Ordinal);
        Assert.False(definition.HasFullVisibility);
    }

    [Fact]
    public void IsRuleWarning_RuleReferencedInWarning_ReturnsTrue()
    {
        // Arrange
        var rule = new BoardRuleDto(1, "Incomplete rule", "When item added", string.Empty, true);
        var warnings = BoardRulesWarningAnalyser.AnalyseWarnings(CreateDefinition([rule]));

        // Act
        var result = BoardRulesWarningAnalyser.IsRuleWarning(rule, warnings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRuleWarning_RuleNotReferencedInWarning_ReturnsFalse()
    {
        // Arrange
        var rule = new BoardRuleDto(1, "Healthy rule", "When item added", "Assign reviewer", true);
        var warnings = BoardRulesWarningAnalyser.AnalyseWarnings(CreateDefinition([rule]));

        // Act
        var result = BoardRulesWarningAnalyser.IsRuleWarning(rule, warnings);

        // Assert
        Assert.False(result);
    }

    private static BoardRulesDefinitionDto CreateDefinition(IReadOnlyList<BoardRuleDto> rules)
        => new(
            "PVT_alpha",
            "Alpha Board",
            "owner",
            [
                new BoardColumnDto(0, "To Do", 0, ["To Do"]),
                new BoardColumnDto(1, "Done", 1, ["Done"]),
            ],
            rules,
            []);
}
