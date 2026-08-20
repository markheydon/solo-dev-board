namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Daily Focus snapshot of pull requests stalled awaiting review.</summary>
/// <param name="Items">Stalled pull requests, oldest first.</param>
/// <param name="UsedInReviewColumn">
/// <see langword="true"/> when detection used time in an In Review (or equivalent) Status column;
/// otherwise pending-review catalogue fallback was used.
/// </param>
public sealed record DailyFocusStalledReviewSnapshotDto(
    IReadOnlyList<DailyFocusStalledReviewPullRequestDto> Items,
    bool UsedInReviewColumn);
