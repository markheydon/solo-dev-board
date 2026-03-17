namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a label synchronisation preview at the Application-to-App boundary.</summary>
/// <param name="ToAdd">The labels to add to the target repository.</param>
/// <param name="ToUpdate">The labels to update in the target repository.</param>
/// <param name="ToDelete">The labels to delete from the target repository.</param>
/// <param name="Skipped">The labels skipped because they already match exactly.</param>
public sealed record LabelSyncPreviewDto(
    IReadOnlyList<LabelDto> ToAdd,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<LabelDto> ToDelete,
    IReadOnlyList<LabelDto> Skipped);
