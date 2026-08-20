namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Grouped Backlog Review items plus any partial catalogue failures.</summary>
/// <param name="Urgent">Items labelled <c>priority/high</c> or <c>priority/critical</c>.</param>
/// <param name="ReadyToStart">Unblocked items that are not already Up Next or In Progress.</param>
/// <param name="BlockedOrDeferred">Items parked by <c>status/blocked</c>, <c>status/ice-box</c>, or matching board Status.</param>
/// <param name="Failures">
/// Per-repository catalogue failures. Grouping still proceeds when this list is non-empty and
/// the remaining items produced groups.
/// </param>
public sealed record BacklogReviewResultDto(
    IReadOnlyList<BacklogReviewItemDto> Urgent,
    IReadOnlyList<BacklogReviewItemDto> ReadyToStart,
    IReadOnlyList<BacklogReviewItemDto> BlockedOrDeferred,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> Failures);
