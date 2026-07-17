namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents the outcome of comparing two board rules definitions.</summary>
/// <param name="LeftDefinition">The primary board rules definition.</param>
/// <param name="RightDefinition">The comparison board rules definition.</param>
/// <param name="Differences">The detected differences between the two definitions.</param>
public sealed record BoardRulesComparisonResultDto(
    BoardRulesDefinitionDto LeftDefinition,
    BoardRulesDefinitionDto RightDefinition,
    IReadOnlyList<BoardRulesComparisonDifferenceDto> Differences)
{
    /// <summary>Gets a value indicating whether any differences were detected.</summary>
    public bool HasDifferences => Differences.Count > 0;
}
