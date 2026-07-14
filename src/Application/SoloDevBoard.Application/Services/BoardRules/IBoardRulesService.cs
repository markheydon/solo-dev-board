namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Provides board-rules metadata for the Board Rules Visualiser.</summary>
public interface IBoardRulesService
{
    /// <summary>Retrieves the supported board rules metadata for the specified GitHub Project v2 board.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The board rules metadata read model.</returns>
    Task<BoardRulesDefinitionDto> GetBoardRulesAsync(string owner, string repo, string projectId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves supported GitHub Project v2 boards linked to the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of supported project board options for the repository.</returns>
    Task<IReadOnlyList<BoardRulesProjectBoardOptionDto>> GetProjectBoardOptionsAsync(string owner, string repo, CancellationToken cancellationToken = default);
}
