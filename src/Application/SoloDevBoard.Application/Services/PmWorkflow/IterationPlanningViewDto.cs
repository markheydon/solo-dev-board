namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Read model for the Iteration Planning page.</summary>
/// <param name="UpNextItems">Items currently in Up Next on the selected planning board.</param>
/// <param name="Candidates">Work items from included repositories that are not already Up Next or In Progress.</param>
/// <param name="CatalogueFailures">Per-repository catalogue failures that did not prevent loading candidates.</param>
/// <param name="HasFocusOrderField"><see langword="true" /> when the selected board exposes a Focus Order field.</param>
/// <param name="NextStoryFocusOrder">The next sequential Focus Order value for story, enabler, and test cards.</param>
/// <param name="ActiveLoad">The count of Up Next plus In Progress items on the board.</param>
/// <param name="Capacity">The resolved planning capacity limit.</param>
/// <param name="IsAtOrOverCapacity"><see langword="true" /> when <paramref name="ActiveLoad"/> is at or above <paramref name="Capacity"/>.</param>
/// <param name="StalledUpNextItems">Up Next items that have exceeded the stall threshold.</param>
public sealed record IterationPlanningViewDto(
    IReadOnlyList<IterationPlanningUpNextItemDto> UpNextItems,
    IReadOnlyList<IterationPlanningCandidateDto> Candidates,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> CatalogueFailures,
    bool HasFocusOrderField,
    double NextStoryFocusOrder,
    int ActiveLoad,
    int Capacity,
    bool IsAtOrOverCapacity,
    IReadOnlyList<IterationPlanningStalledItemDto> StalledUpNextItems);
