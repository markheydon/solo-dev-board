using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Default implementation of <see cref="IRepositoryService"/>.</summary>
public sealed class RepositoryService : IRepositoryService
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="RepositoryService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to fetch repository data.</param>
    public RepositoryService(IGitHubService gitHubService)
    {
        ArgumentNullException.ThrowIfNull(gitHubService);
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await _gitHubService.GetRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        return repositories.Select(MapToDto).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryDto>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await _gitHubService.GetActiveRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        return repositories.Select(MapToDto).ToArray();
    }

    /// <summary>Maps a domain repository record to the application DTO shape.</summary>
    /// <param name="repository">The domain repository to map.</param>
    /// <returns>A mapped application repository DTO.</returns>
    private static RepositoryDto MapToDto(Repository repository)
        => new(
            repository.Id,
            repository.Name,
            repository.FullName,
            repository.Description,
            repository.Url,
            repository.IsPrivate,
            repository.IsArchived,
            repository.CreatedAt,
            repository.UpdatedAt);
}