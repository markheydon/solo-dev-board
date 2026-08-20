namespace SoloDevBoard.Domain.Entities.Migration;

/// <summary>Represents Projects v2 board discovery for a repository, including visibility metadata.</summary>
public sealed record ProjectBoardDiscovery
{
    /// <summary>Gets supported project boards that expose a Status field with options.</summary>
    public IReadOnlyList<ProjectBoardStatusStructure> SupportedBoards { get; init; } = [];

    /// <summary>Gets the total number of linked project boards reported by GitHub.</summary>
    public int TotalLinkedProjectCount { get; init; }

    /// <summary>Gets the number of linked project boards that are inaccessible to the current credentials.</summary>
    public int InaccessibleLinkedProjectCount { get; init; }
}
