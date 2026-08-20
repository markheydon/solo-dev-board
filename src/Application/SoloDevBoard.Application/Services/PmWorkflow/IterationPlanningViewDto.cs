namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Read model for the Iteration Planning page.</summary>
/// <param name="UpNextItems">Items currently in Up Next on the selected planning board.</param>
/// <param name="Candidates">Work items from included repositories that are not already Up Next or In Progress.</param>
/// <param name="CatalogueFailures">Per-repository catalogue failures that did not prevent loading candidates.</param>
public sealed record IterationPlanningViewDto(
    IReadOnlyList<IterationPlanningUpNextItemDto> UpNextItems,
    IReadOnlyList<IterationPlanningCandidateDto> Candidates,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> CatalogueFailures);
