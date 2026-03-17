namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a label synchronisation preview for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="ToCreate">The labels to create in the target repository.</param>
/// <param name="ToUpdate">The labels to update in the target repository.</param>
/// <param name="ToDelete">The labels to delete from the target repository.</param>
/// <param name="Skipped">The labels skipped because they already match exactly.</param>
public sealed record LabelSyncRepositoryPreviewDto(
    string RepositoryFullName,
    IReadOnlyList<LabelDto> ToCreate,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<LabelDto> ToDelete,
    IReadOnlyList<LabelDto> Skipped);
