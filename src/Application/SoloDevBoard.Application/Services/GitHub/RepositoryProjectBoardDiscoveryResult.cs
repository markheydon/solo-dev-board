using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Result of discovering GitHub Project v2 boards linked to a repository.</summary>
/// <param name="SupportedProjectBoards">Project boards that expose a supported Status field.</param>
/// <param name="TotalLinkedProjectCount">The number of project boards GitHub reports as linked to the repository.</param>
/// <param name="InaccessibleLinkedProjectCount">Linked project boards that could not be read with the current credentials.</param>
public sealed record RepositoryProjectBoardDiscoveryResult(
    IReadOnlyList<TriageProjectBoard> SupportedProjectBoards,
    int TotalLinkedProjectCount,
    int InaccessibleLinkedProjectCount);
