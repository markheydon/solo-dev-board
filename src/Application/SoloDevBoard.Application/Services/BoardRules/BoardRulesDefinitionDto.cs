using System.Collections.Generic;

namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents the board rules metadata returned by the Application layer.</summary>
public sealed record BoardRulesDefinitionDto(
    string ProjectId,
    string ProjectTitle,
    string OwnerLogin,
    IReadOnlyList<BoardColumnDto> Columns,
    IReadOnlyList<BoardRuleDto> Rules,
    IReadOnlyList<string> UnsupportedDetails)
{
    /// <summary>Gets a value indicating whether the board metadata is fully visible.</summary>
    public bool HasFullVisibility => UnsupportedDetails.Count == 0;
}
