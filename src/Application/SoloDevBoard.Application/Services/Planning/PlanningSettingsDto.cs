namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Cross-Repo Planning preferences persisted in browser storage.</summary>
/// <param name="PlanningBoardNodeId">The selected Projects v2 board node identifier, or <see langword="null"/> when unset.</param>
/// <param name="ExcludedRepositories">Repositories excluded from PM queries, in <c>owner/name</c> form.</param>
/// <param name="Capacity">The maximum active load (Up Next plus In Progress) for planning.</param>
/// <param name="StallDays">Days before Daily Focus treats a review pull request as stalled.</param>
/// <param name="NeglectDays">Days before a repository is treated as neglected.</param>
/// <param name="LimitRecommendationsToPlanningBoard">
/// When <see langword="true"/>, Daily Focus Recommended today ranks only items on the selected planning board.
/// </param>
public sealed record PlanningSettingsDto(
    string? PlanningBoardNodeId,
    IReadOnlyList<string> ExcludedRepositories,
    int Capacity,
    int StallDays,
    int NeglectDays,
    bool LimitRecommendationsToPlanningBoard = false);
