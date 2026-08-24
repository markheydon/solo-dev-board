namespace SoloDevBoard.App.Components.Features.Labels.Dialogs;

/// <summary>Represents one label and the repositories where it will be removed.</summary>
/// <param name="LabelName">The label name to delete.</param>
/// <param name="RepositoryFullNames">The owner/repository full names where the label will be removed.</param>
public sealed record LabelBulkDeleteConfirmDialogLabelTarget(
    string LabelName,
    IReadOnlyList<string> RepositoryFullNames);

/// <summary>Represents the payload for the bulk label delete confirmation dialog.</summary>
/// <param name="Targets">Per-label delete targets showing affected repositories.</param>
public sealed record LabelBulkDeleteConfirmDialogRequest(
    IReadOnlyList<LabelBulkDeleteConfirmDialogLabelTarget> Targets);
