using SoloDevBoard.Application.Services.BoardRules;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="BoardRulesComparer"/>.</summary>
public sealed class BoardRulesComparerTests
{
    [Fact]
    public void Compare_IdenticalDefinitions_ReturnsNoDifferences()
    {
        // Arrange
        var left = CreateDefinition(
            ["To Do", "In Progress", "Done"],
            [new BoardRuleDto(1, "Auto-assign", "When item added", "Assign reviewer", true)]);
        var right = CreateDefinition(
            ["To Do", "In Progress", "Done"],
            [new BoardRuleDto(2, "Auto-assign", "When item added", "Assign reviewer", true)]);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.False(result.HasDifferences);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public void Compare_MissingColumnOnComparisonBoard_ReturnsMissingDifference()
    {
        // Arrange
        var left = CreateDefinition(["To Do", "In Progress", "Done"], []);
        var right = CreateDefinition(["To Do", "Done"], []);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.Contains(result.Differences, difference =>
            difference.Category == "Column"
            && difference.DifferenceType == "Missing in comparison board"
            && difference.Name == "In Progress");
    }

    [Fact]
    public void Compare_ExtraColumnOnComparisonBoard_ReturnsExtraDifference()
    {
        // Arrange
        var left = CreateDefinition(["To Do", "Done"], []);
        var right = CreateDefinition(["To Do", "Review", "Done"], []);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.Contains(result.Differences, difference =>
            difference.Category == "Column"
            && difference.DifferenceType == "Extra on comparison board"
            && difference.Name == "Review");
    }

    [Fact]
    public void Compare_ColumnOrderChanged_ReturnsOrderDifference()
    {
        // Arrange
        var left = CreateDefinition(["To Do", "In Progress", "Done"], []);
        var right = CreateDefinition(["To Do", "Done", "In Progress"], []);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.Contains(result.Differences, difference =>
            difference.Category == "Column"
            && difference.DifferenceType == "Order changed");
    }

    [Fact]
    public void Compare_ChangedRuleConfiguration_ReturnsChangedDifference()
    {
        // Arrange
        var left = CreateDefinition(
            ["To Do", "Done"],
            [new BoardRuleDto(1, "Auto-assign", "When item added", "Assign reviewer", true)]);
        var right = CreateDefinition(
            ["To Do", "Done"],
            [new BoardRuleDto(2, "Auto-assign", "When item added", "Close issue", true)]);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.Contains(result.Differences, difference =>
            difference.Category == "Rule"
            && difference.DifferenceType == "Changed"
            && difference.Name == "Auto-assign");
    }

    [Fact]
    public void Compare_PartialVisibilityDiffers_ReturnsVisibilityDifference()
    {
        // Arrange
        var left = CreateDefinition(["To Do", "Done"], [], unsupportedDetails: []);
        var right = CreateDefinition(
            ["To Do", "Done"],
            [],
            unsupportedDetails: ["Board automation rules are not yet available through the current GitHub query model."]);

        // Act
        var result = BoardRulesComparer.Compare(left, right);

        // Assert
        Assert.Contains(result.Differences, difference =>
            difference.Category == "Visibility"
            && difference.DifferenceType == "Changed");
    }

    private static BoardRulesDefinitionDto CreateDefinition(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<BoardRuleDto> rules,
        IReadOnlyList<string>? unsupportedDetails = null)
    {
        var columns = columnNames
            .Select((name, index) => new BoardColumnDto(index, name, index, [name]))
            .ToArray();

        return new BoardRulesDefinitionDto(
            "PVT_alpha",
            "Alpha Board",
            "owner",
            columns,
            rules,
            unsupportedDetails ?? []);
    }
}
