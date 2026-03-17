namespace SoloDevBoard.App.Components.Features.Labels.Dialogs;

/// <summary>Defines the available label management operation modes.</summary>
public enum LabelOperationMode
{
    /// <summary>Represents creating a new label.</summary>
    Create,

    /// <summary>Represents editing an existing label.</summary>
    Edit,

    /// <summary>Represents deleting an existing label.</summary>
    Delete,
}

/// <summary>Represents input data used to render a label operation dialog.</summary>
/// <param name="Mode">The dialog operation mode.</param>
/// <param name="OriginalLabelName">The original label name for edit or delete operations.</param>
/// <param name="LabelName">The initial label name value.</param>
/// <param name="Colour">The initial hexadecimal colour value.</param>
/// <param name="Description">The initial label description.</param>
/// <param name="AvailableRepositories">The full repository names available for selection.</param>
/// <param name="SelectableRepositories">The full repository names that can be selected for the operation.</param>
/// <param name="SelectedRepositories">The full repository names selected by default.</param>
public sealed record LabelOperationDialogRequest(
    LabelOperationMode Mode,
    string OriginalLabelName,
    string LabelName,
    string Colour,
    string Description,
    IReadOnlyList<string> AvailableRepositories,
    IReadOnlyList<string> SelectableRepositories,
    IReadOnlyList<string> SelectedRepositories);

/// <summary>Represents the output from a label operation dialog.</summary>
/// <param name="Mode">The confirmed operation mode.</param>
/// <param name="OriginalLabelName">The original label name for edit or delete operations.</param>
/// <param name="LabelName">The submitted label name.</param>
/// <param name="Colour">The submitted hexadecimal colour value.</param>
/// <param name="Description">The submitted label description.</param>
/// <param name="SelectedRepositories">The repositories selected for the operation.</param>
public sealed record LabelOperationDialogResult(
    LabelOperationMode Mode,
    string OriginalLabelName,
    string LabelName,
    string Colour,
    string Description,
    IReadOnlyList<string> SelectedRepositories);