namespace SoloDevBoard.Domain.Entities.Migration;

/// <summary>Represents the Status field structure for a Projects v2 board.</summary>
public sealed record ProjectBoardStatusStructure
{
    /// <summary>Gets the GitHub node identifier for the project board.</summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>Gets the project board title.</summary>
    public string ProjectTitle { get; init; } = string.Empty;

    /// <summary>Gets the Status field node identifier for the board.</summary>
    public string StatusFieldId { get; init; } = string.Empty;

    /// <summary>Gets the Status field options in board-defined order.</summary>
    public IReadOnlyList<ProjectBoardStatusStructureOption> Options { get; init; } = [];
}
