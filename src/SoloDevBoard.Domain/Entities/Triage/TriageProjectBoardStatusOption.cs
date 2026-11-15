namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a selectable status option within a GitHub Project v2 status field.</summary>
public sealed record TriageProjectBoardStatusOption
{
    /// <summary>Gets the GitHub node identifier for the status option.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display name of the status option.</summary>
    public string Name { get; init; } = string.Empty;
}
