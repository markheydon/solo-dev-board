namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a milestone synchronisation preview for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="ToCreate">The milestones to create in the target repository.</param>
/// <param name="ToUpdate">The milestones to update in the target repository.</param>
/// <param name="ToDelete">The milestones to delete from the target repository.</param>
/// <param name="Skipped">The milestones skipped by conflict strategy rules.</param>
public sealed record MilestoneSyncRepositoryPreviewDto(
    string RepositoryFullName,
    IReadOnlyList<MilestoneDto> ToCreate,
    IReadOnlyList<MilestoneDto> ToUpdate,
    IReadOnlyList<MilestoneDto> ToDelete,
    IReadOnlyList<MilestoneDto> Skipped);
