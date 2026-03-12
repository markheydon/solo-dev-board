namespace SoloDevBoard.Application.Services;

/// <summary>Represents a milestone synchronisation preview at the Application-to-App boundary.</summary>
/// <param name="ToCreate">The milestones to create in the target repository.</param>
/// <param name="ToUpdate">The milestones to update in the target repository.</param>
/// <param name="ToDelete">The milestones to delete from the target repository.</param>
/// <param name="Skipped">The milestones skipped by conflict strategy rules.</param>
public sealed record MilestoneSyncPreviewDto(
    IReadOnlyList<MilestoneDto> ToCreate,
    IReadOnlyList<MilestoneDto> ToUpdate,
    IReadOnlyList<MilestoneDto> ToDelete,
    IReadOnlyList<MilestoneDto> Skipped);
