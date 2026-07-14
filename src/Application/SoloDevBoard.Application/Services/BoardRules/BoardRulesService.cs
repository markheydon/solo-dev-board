using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Provides a default implementation of <see cref="IBoardRulesService"/>.</summary>
public sealed class BoardRulesService : IBoardRulesService
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="BoardRulesService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to retrieve board data.</param>
    public BoardRulesService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
    }

    /// <inheritdoc/>
    public async Task<BoardRulesDefinitionDto> GetBoardRulesAsync(string owner, string repo, string projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        return await _gitHubService
            .GetBoardRulesDefinitionAsync(owner, repo, projectId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BoardRulesProjectBoardOptionDto>> GetProjectBoardOptionsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var projectBoards = await _gitHubService
            .GetProjectBoardsForRepositoryAsync(owner, repo, cancellationToken)
            .ConfigureAwait(false);

        return projectBoards
            .Select(projectBoard => new BoardRulesProjectBoardOptionDto(
                projectBoard.Id,
                projectBoard.Title,
                projectBoard.OwnerLogin))
            .OrderBy(projectBoard => projectBoard.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
