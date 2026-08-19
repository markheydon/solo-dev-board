namespace SoloDevBoard.Domain.Entities.PmWorkflow;

/// <summary>Represents a Status single-select option discovered on a Projects v2 board.</summary>
public sealed record ProjectBoardStatusOption
{
    /// <summary>Gets the status option node identifier.</summary>
    public string OptionId { get; init; } = string.Empty;

    /// <summary>Gets the status option display name.</summary>
    public string Name { get; init; } = string.Empty;
}
