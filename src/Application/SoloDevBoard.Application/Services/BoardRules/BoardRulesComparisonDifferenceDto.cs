namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents a single difference detected when comparing two board rules definitions.</summary>
/// <param name="Category">The comparison category, such as column or rule.</param>
/// <param name="DifferenceType">The difference type, such as missing, extra, or changed.</param>
/// <param name="Name">The display name of the affected column or rule.</param>
/// <param name="Details">Additional context describing the difference.</param>
public sealed record BoardRulesComparisonDifferenceDto(
    string Category,
    string DifferenceType,
    string Name,
    string Details);
