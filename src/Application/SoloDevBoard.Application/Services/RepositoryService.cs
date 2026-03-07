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
    public Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
        => _gitHubService.GetRepositoriesAsync(cancellationToken);
}