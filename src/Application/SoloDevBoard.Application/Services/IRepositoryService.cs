namespace SoloDevBoard.Application.Services;

/// <summary>Provides repository listing operations for the authenticated user.</summary>
public interface IRepositoryService
{
    /// <summary>Retrieves repositories accessible to the authenticated GitHub user.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repository DTOs visible to the authenticated user.</returns>
    Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves active repositories accessible to the authenticated GitHub user.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of non-archived repository DTOs visible to the authenticated user.</returns>
    Task<IReadOnlyList<RepositoryDto>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default);
}