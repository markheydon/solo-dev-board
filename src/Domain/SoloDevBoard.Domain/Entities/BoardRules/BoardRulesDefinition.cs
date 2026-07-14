using System.Collections.Generic;

namespace SoloDevBoard.Domain.Entities.BoardRules;

/// <summary>Represents the supported board rules metadata for a GitHub Project v2 board.</summary>
public sealed record BoardRulesDefinition
{
    /// <summary>Gets the GitHub node identifier for the project board.</summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>Gets the title of the project board.</summary>
    public string ProjectTitle { get; init; } = string.Empty;

    /// <summary>Gets the login of the project board owner.</summary>
    public string OwnerLogin { get; init; } = string.Empty;

    /// <summary>Gets the board columns derived from the status field.</summary>
    public IReadOnlyList<BoardColumn> Columns { get; init; } = [];

    /// <summary>Gets the automation rules for the board.</summary>
    public IReadOnlyList<BoardRule> Rules { get; init; } = [];

    /// <summary>Gets explicit details when board visibility is partial or unsupported.</summary>
    public IReadOnlyList<string> UnsupportedDetails { get; init; } = [];

    /// <summary>Gets a value indicating whether the board metadata is fully visible.</summary>
    public bool HasFullVisibility => UnsupportedDetails.Count == 0;
}
