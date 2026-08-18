namespace SoloDevBoard.Domain.Entities.PmWorkflow;

/// <summary>Represents the Status single-select value on a project board item.</summary>
public sealed record ProjectBoardItemStatus
{
    /// <summary>Gets the status option node identifier.</summary>
    public string OptionId { get; init; } = string.Empty;

    /// <summary>Gets the status option display name.</summary>
    public string Name { get; init; } = string.Empty;
}
