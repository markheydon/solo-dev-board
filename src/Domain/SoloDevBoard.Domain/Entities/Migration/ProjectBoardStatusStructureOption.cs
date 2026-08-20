namespace SoloDevBoard.Domain.Entities.Migration;

/// <summary>Represents a Projects v2 Status single-select option within a board structure.</summary>
public sealed record ProjectBoardStatusStructureOption
{
    /// <summary>Gets the GitHub node identifier for the status option.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display name of the status option.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the GitHub single-select colour enum value (for example, GRAY or BLUE).</summary>
    public string Colour { get; init; } = "GRAY";

    /// <summary>Gets the plain-text description of the status option.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the zero-based display order of the option on the board.</summary>
    public int Order { get; init; }
}
