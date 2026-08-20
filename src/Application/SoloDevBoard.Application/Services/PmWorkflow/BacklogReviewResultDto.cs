namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Grouped Backlog Review items plus any partial catalogue failures.</summary>
/// <param name="Urgent">Items labelled <c>priority/high</c> or <c>priority/critical</c>.</param>
/// <param name="ReadyToStart">Unblocked, fully labelled items that are not urgent and not already Up Next or In Progress.</param>
/// <param name="AwaitingTriage">Items missing a core <c>type/</c> or <c>priority/</c> label.</param>
/// <param name="BlockedOrDeferred">Items parked by <c>status/blocked</c>, <c>status/ice-box</c>, or matching board Status.</param>
/// <param name="EpicsNearComplete">Open epics or features whose tracked sub-issues are all closed.</param>
/// <param name="NeglectedRepositories">Included repositories with no issue or pull request activity within the neglect threshold.</param>
/// <param name="SubIssueCountsUnavailable">
/// <see langword="true"/> when open epics or features exist but GitHub did not return sub-issue counts for any of them.
/// </param>
/// <param name="Failures">
/// Per-repository catalogue failures. Grouping still proceeds when this list is non-empty and
/// the remaining items produced groups.
/// </param>
public sealed record BacklogReviewResultDto(
    IReadOnlyList<BacklogReviewItemDto> Urgent,
    IReadOnlyList<BacklogReviewItemDto> ReadyToStart,
    IReadOnlyList<BacklogReviewItemDto> AwaitingTriage,
    IReadOnlyList<BacklogReviewItemDto> BlockedOrDeferred,
    IReadOnlyList<BacklogEpicNearCompleteItemDto> EpicsNearComplete,
    IReadOnlyList<BacklogNeglectedRepositoryDto> NeglectedRepositories,
    bool SubIssueCountsUnavailable,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> Failures);
