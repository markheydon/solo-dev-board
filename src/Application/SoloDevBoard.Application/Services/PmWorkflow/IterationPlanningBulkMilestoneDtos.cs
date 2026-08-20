namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Milestone title option for bulk assignment across selected Up Next items.</summary>
/// <param name="Title">The milestone title shared across one or more repositories.</param>
/// <param name="RepositoryFullNames">Repositories in the current selection that expose this milestone title.</param>
public sealed record IterationPlanningMilestoneOptionDto(
    string Title,
    IReadOnlyList<string> RepositoryFullNames);

/// <summary>Outcome of applying a milestone to a checked Up Next batch.</summary>
/// <param name="AppliedCount">Items that received the milestone.</param>
/// <param name="SkippedRepositories">Repositories skipped because the milestone title was missing.</param>
public sealed record IterationPlanningBulkMilestoneResultDto(
    int AppliedCount,
    IReadOnlyList<string> SkippedRepositories);
