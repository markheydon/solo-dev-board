using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Provides repository operations for managing GitHub milestones.</summary>
public interface IMilestoneRepository
{
    /// <summary>Retrieves all milestones for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of milestones for the repository.</returns>
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Creates a new milestone in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="milestone">The milestone details to create.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created milestone.</returns>
    Task<Milestone> CreateMilestoneAsync(string owner, string repo, Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing milestone in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="milestoneNumber">The existing milestone number to update.</param>
    /// <param name="milestone">The new milestone details to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated milestone.</returns>
    Task<Milestone> UpdateMilestoneAsync(string owner, string repo, int milestoneNumber, Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>Deletes a milestone from the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="milestoneNumber">The milestone number to delete.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteMilestoneAsync(string owner, string repo, int milestoneNumber, CancellationToken cancellationToken = default);
}
