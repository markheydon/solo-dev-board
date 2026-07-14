using SoloDevBoard.Application.Services.BoardRules;

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
}
