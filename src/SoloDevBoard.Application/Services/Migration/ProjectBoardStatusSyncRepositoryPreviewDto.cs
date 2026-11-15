namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a Projects v2 Status column synchronisation preview for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="TargetProjectId">The selected target project board identifier, or <see langword="null"/> when creating a new board.</param>
/// <param name="CreateNewBoard">A value indicating whether a new linked board will be created.</param>
/// <param name="ToCreate">The Status options to create on the target board.</param>
/// <param name="ToUpdate">The Status options to update on the target board.</param>
/// <param name="ToDelete">The Status options to remove from the target board.</param>
/// <param name="Skipped">The Status options skipped by conflict strategy rules.</param>
/// <param name="Warnings">Warning messages for operations that cannot be completed safely.</param>
/// <param name="TotalLinkedProjectCount">The total number of linked project boards reported by GitHub.</param>
/// <param name="InaccessibleLinkedProjectCount">The number of linked project boards that are inaccessible to the current credentials.</param>
public sealed record ProjectBoardStatusSyncRepositoryPreviewDto(
    string RepositoryFullName,
    string? TargetProjectId,
    bool CreateNewBoard,
    IReadOnlyList<ProjectBoardStatusOptionDto> ToCreate,
    IReadOnlyList<ProjectBoardStatusOptionDto> ToUpdate,
    IReadOnlyList<ProjectBoardStatusOptionDto> ToDelete,
    IReadOnlyList<ProjectBoardStatusOptionDto> Skipped,
    IReadOnlyList<string> Warnings,
    int TotalLinkedProjectCount,
    int InaccessibleLinkedProjectCount);
