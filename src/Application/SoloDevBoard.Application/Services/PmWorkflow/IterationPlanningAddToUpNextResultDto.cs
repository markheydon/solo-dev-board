namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Outcome of moving a work item to Up Next during Iteration Planning.</summary>
/// <param name="AddedBoardCard"><see langword="true" /> when the item was added to the board before Status was set.</param>
/// <param name="FocusOrderAssigned">The Focus Order written for story, enabler, and test cards; otherwise, <see langword="null" />.</param>
/// <param name="FocusOrderSkipped">Whether Focus Order was intentionally skipped for the item type.</param>
public sealed record IterationPlanningAddToUpNextResultDto(
    bool AddedBoardCard,
    double? FocusOrderAssigned,
    bool FocusOrderSkipped);
