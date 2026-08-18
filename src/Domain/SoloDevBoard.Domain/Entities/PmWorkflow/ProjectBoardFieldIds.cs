namespace SoloDevBoard.Domain.Entities.PmWorkflow;

/// <summary>Discovered GitHub Project v2 field identifiers for PM workflow board operations.</summary>
public sealed record ProjectBoardFieldIds
{
    /// <summary>Gets the Status single-select field node identifier.</summary>
    public string StatusFieldId { get; init; } = string.Empty;

    /// <summary>Gets the Focus Order number field node identifier when the board exposes that field.</summary>
    public string? FocusOrderFieldId { get; init; }
}
