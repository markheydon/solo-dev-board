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
    /// <returns>Supported project board options and visibility metadata for the repository.</returns>
    Task<BoardRulesProjectBoardDiscoveryDto> GetProjectBoardOptionsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Compares two board rules definitions and returns structured differences.</summary>
    /// <param name="left">The primary board rules definition.</param>
    /// <param name="right">The comparison board rules definition.</param>
    /// <returns>A comparison result describing missing, extra, and changed board structure.</returns>
    BoardRulesComparisonResultDto CompareBoardRules(BoardRulesDefinitionDto left, BoardRulesDefinitionDto right);
}
