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
/// <param name="Failures">Per-item assignment failures after earlier items succeeded.</param>
public sealed record IterationPlanningBulkMilestoneResultDto(
    int AppliedCount,
    IReadOnlyList<string> SkippedRepositories,
    IReadOnlyList<IterationPlanningBulkMilestoneFailureDto> Failures);

/// <summary>Per-item failure while applying a bulk milestone assignment.</summary>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Number">The repository-scoped item number.</param>
/// <param name="Message">The error message returned for this item.</param>
public sealed record IterationPlanningBulkMilestoneFailureDto(
    string RepositoryFullName,
    int Number,
    string Message);
