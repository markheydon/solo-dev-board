using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides repository listing operations for the authenticated user.</summary>
public interface IRepositoryService
{
    /// <summary>Retrieves active repositories accessible to the authenticated GitHub user.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of non-archived repositories visible to the authenticated user.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default);
}