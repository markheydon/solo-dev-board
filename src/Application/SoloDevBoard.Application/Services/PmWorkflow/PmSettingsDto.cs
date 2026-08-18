namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Cross-Repo PM Workflow preferences persisted in browser storage.</summary>
/// <param name="PlanningBoardNodeId">The selected Projects v2 board node identifier, or <see langword="null"/> when unset.</param>
/// <param name="ExcludedRepositories">Repositories excluded from PM queries, in <c>owner/name</c> form.</param>
/// <param name="Capacity">The maximum active load (Up Next plus In Progress) for planning.</param>
/// <param name="StallDays">Days before an Up Next item is treated as stalled.</param>
/// <param name="NeglectDays">Days before a repository is treated as neglected.</param>
public sealed record PmSettingsDto(
    string? PlanningBoardNodeId,
    IReadOnlyList<string> ExcludedRepositories,
    int Capacity,
    int StallDays,
    int NeglectDays);
