namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Read model for the Iteration Planning page.</summary>
/// <param name="UpNextItems">Items currently in Up Next on the selected planning board.</param>
/// <param name="Candidates">Work items from included repositories that are not already Up Next or In Progress.</param>
/// <param name="CatalogueFailures">Per-repository catalogue failures that did not prevent loading candidates.</param>
/// <param name="HasFocusOrderField"><see langword="true" /> when the selected board exposes a Focus Order field.</param>
/// <param name="NextStoryFocusOrder">The next sequential Focus Order value for story, enabler, and test cards.</param>
public sealed record IterationPlanningViewDto(
    IReadOnlyList<IterationPlanningUpNextItemDto> UpNextItems,
    IReadOnlyList<IterationPlanningCandidateDto> Candidates,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> CatalogueFailures,
    bool HasFocusOrderField,
    double NextStoryFocusOrder);
