namespace SoloDevBoard.App.Components.Features.Labels.Dialogs;

/// <summary>Represents the payload for the bulk label delete confirmation dialog.</summary>
/// <param name="LabelNames">The label names that will be deleted.</param>
/// <param name="RepositoryFullNames">The repositories in scope for the bulk delete.</param>
public sealed record LabelBulkDeleteConfirmDialogRequest(
    IReadOnlyList<string> LabelNames,
    IReadOnlyList<string> RepositoryFullNames);
