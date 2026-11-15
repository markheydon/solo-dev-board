using SoloDevBoard.Domain.Entities.Migration;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Provides repository operations for Projects v2 Status field structure.</summary>
public interface IProjectBoardStructureRepository
{
    /// <summary>Discovers linked project boards for a repository, including visibility metadata.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Supported boards and linked-project visibility counts.</returns>
    Task<ProjectBoardDiscovery> DiscoverBoardsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the Status field structure for a project board.</summary>
    /// <param name="projectId">The GitHub node identifier for the project board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Status field structure for the project board.</returns>
    Task<ProjectBoardStatusStructure> GetStatusStructureAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves Status option identifiers that are currently assigned to board items.</summary>
    /// <param name="projectId">The GitHub node identifier for the project board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The set of Status option identifiers in use on the board.</returns>
    Task<IReadOnlySet<string>> GetStatusOptionIdsInUseAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>Creates a repository-linked Projects v2 board.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="title">The title for the new project board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Status field structure for the newly created board.</returns>
    Task<ProjectBoardStatusStructure> CreateLinkedProjectAsync(string owner, string repo, string title, CancellationToken cancellationToken = default);

    /// <summary>Replaces the Status field options on a project board.</summary>
    /// <param name="projectId">The GitHub node identifier for the project board.</param>
    /// <param name="statusFieldId">The Status field node identifier.</param>
    /// <param name="options">The complete Status option list to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated Status field structure.</returns>
    Task<ProjectBoardStatusStructure> UpdateStatusOptionsAsync(
        string projectId,
        string statusFieldId,
        IReadOnlyList<ProjectBoardStatusStructureOption> options,
        CancellationToken cancellationToken = default);
}
