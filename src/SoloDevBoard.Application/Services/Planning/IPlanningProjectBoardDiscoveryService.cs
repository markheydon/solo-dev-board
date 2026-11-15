using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Discovers Projects v2 boards linked to active repositories for Planning.</summary>
public interface IPlanningProjectBoardDiscoveryService
{
    /// <summary>
    /// Returns distinct planning board options discovered across active repositories.
    /// Archived repositories are not scanned.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Distinct board options and linked-board visibility counts.</returns>
    Task<PlanningProjectBoardDiscoveryDto> GetPlanningBoardOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns distinct planning board options discovered for the supplied active repositories.
    /// </summary>
    /// <param name="repositories">Active repositories to scan for linked project boards.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Distinct board options and linked-board visibility counts.</returns>
    Task<PlanningProjectBoardDiscoveryDto> GetPlanningBoardOptionsForRepositoriesAsync(
        IReadOnlyList<RepositoryDto> repositories,
        CancellationToken cancellationToken = default);
}
