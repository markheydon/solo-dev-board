namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a GitHub Project v2 board available for triage placement.</summary>
public sealed record TriageProjectBoard
{
    /// <summary>Gets the GitHub node identifier for the project board.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the project board title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the login of the project owner.</summary>
    public string OwnerLogin { get; init; } = string.Empty;

    /// <summary>Gets the status field node identifier for the board.</summary>
    public string StatusFieldId { get; init; } = string.Empty;

    /// <summary>Gets the available status options for the board.</summary>
    public IReadOnlyList<TriageProjectBoardStatusOption> StatusOptions { get; init; } = [];
}
