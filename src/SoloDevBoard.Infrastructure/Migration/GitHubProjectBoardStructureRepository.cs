using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Domain.Entities.Migration;

namespace SoloDevBoard.Infrastructure.Migration;

/// <summary>GitHub GraphQL implementation of <see cref="IProjectBoardStructureRepository"/>.</summary>
public sealed class GitHubProjectBoardStructureRepository : IProjectBoardStructureRepository
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="GitHubProjectBoardStructureRepository"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used for Projects v2 GraphQL operations.</param>
    public GitHubProjectBoardStructureRepository(IGitHubService gitHubService)
    {
        ArgumentNullException.ThrowIfNull(gitHubService);
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public Task<ProjectBoardDiscovery> DiscoverBoardsAsync(string owner, string repo, CancellationToken cancellationToken = default)
        => _gitHubService.DiscoverProjectBoardStatusStructuresAsync(owner, repo, cancellationToken);

    /// <inheritdoc/>
    public Task<ProjectBoardStatusStructure> GetStatusStructureAsync(string projectId, CancellationToken cancellationToken = default)
        => _gitHubService.GetProjectBoardStatusStructureAsync(projectId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlySet<string>> GetStatusOptionIdsInUseAsync(string projectId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var catalogue = await _gitHubService.GetProjectBoardItemsAsync(projectId, cancellationToken).ConfigureAwait(false);
        return catalogue.Items
            .Where(item => item.Status is not null && !string.IsNullOrWhiteSpace(item.Status.OptionId))
            .Select(item => item.Status!.OptionId)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public Task<ProjectBoardStatusStructure> CreateLinkedProjectAsync(string owner, string repo, string title, CancellationToken cancellationToken = default)
        => _gitHubService.CreateRepositoryLinkedProjectAsync(owner, repo, title, cancellationToken);

    /// <inheritdoc/>
    public Task<ProjectBoardStatusStructure> UpdateStatusOptionsAsync(
        string projectId,
        string statusFieldId,
        IReadOnlyList<ProjectBoardStatusStructureOption> options,
        CancellationToken cancellationToken = default)
        => _gitHubService.UpdateProjectBoardStatusOptionsAsync(projectId, statusFieldId, options, cancellationToken);
}
