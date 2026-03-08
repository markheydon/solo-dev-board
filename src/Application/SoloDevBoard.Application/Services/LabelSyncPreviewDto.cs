namespace SoloDevBoard.Application.Services;

/// <summary>Represents a label synchronisation preview at the Application-to-App boundary.</summary>
/// <param name="ToAdd">The labels to add to the target repository.</param>
/// <param name="ToUpdate">The labels to update in the target repository.</param>
/// <param name="ToDelete">The label names to delete from the target repository.</param>
public sealed record LabelSyncPreviewDto(
    IReadOnlyList<LabelDto> ToAdd,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<string> ToDelete);
