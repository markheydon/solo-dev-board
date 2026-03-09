using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace SoloDevBoard.App.Components.Dialogs;

/// <summary>Provides the code-behind for the label operation dialog component.</summary>
public partial class LabelOperationDialog
{
    private static readonly IReadOnlyList<ColourOption> CommonColourOptions =
    [
        new("Red", "#d73a4a"),
        new("Orange", "#fb8500"),
        new("Yellow", "#d4c441"),
        new("Green", "#2da44e"),
        new("Teal", "#0a9396"),
        new("Blue", "#0969da"),
        new("Purple", "#8250df"),
        new("Pink", "#bf3989"),
        new("Grey", "#8c959f"),
        new("Black", "#24292f"),
    ];

    /// <summary>Gets or sets the dialog request payload.</summary>
    [Parameter]
    public LabelOperationDialogRequest Content { get; set; } = new(
        LabelOperationMode.Create,
        string.Empty,
        string.Empty,
        "#ededed",
        string.Empty,
        [],
        [],
        []);

    /// <summary>Gets or sets the active Fluent dialog instance.</summary>
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    private readonly LabelOperationFormModel model = new();
    private HashSet<string> selectedRepositoryNames = new(StringComparer.OrdinalIgnoreCase);
    private string? validationMessage;
    private bool showColourSelector;
    private string SubmitButtonText => Content.Mode == LabelOperationMode.Edit ? "Save" : "Create";

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        var selectableRepositories = Content.SelectableRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectableRepositories.Count == 0)
        {
            selectableRepositories = Content.AvailableRepositories
                .Where(repository => !string.IsNullOrWhiteSpace(repository))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        model.LabelName = Content.LabelName;
        model.Colour = string.IsNullOrWhiteSpace(Content.Colour) ? "#ededed" : Content.Colour;
        model.Description = Content.Description;

        selectedRepositoryNames = Content.SelectedRepositories
            .Where(repository => selectableRepositories.Contains(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedRepositoryNames.Count == 0)
        {
            selectedRepositoryNames = selectableRepositories;
        }

        validationMessage = null;
        showColourSelector = false;
    }

    private void ToggleColourSelector()
    {
        showColourSelector = !showColourSelector;
    }

    private void SelectPresetColour(string hex)
    {
        model.Colour = hex;
    }

    private void OnCustomColourChanged(ChangeEventArgs args)
    {
        if (args.Value is string value && !string.IsNullOrWhiteSpace(value))
        {
            model.Colour = value;
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (!TryValidateSelection())
        {
            return;
        }

        await CloseDialogAsync();
    }

    private async Task ConfirmDeleteAsync()
    {
        if (!TryValidateSelection())
        {
            return;
        }

        await CloseDialogAsync();
    }

    private async Task CancelAsync()
    {
        await Dialog.CancelAsync();
    }

    private void OnRepositoryToggleChanged(string repository, ChangeEventArgs args)
    {
        if (!CanSelectRepository(repository))
        {
            return;
        }

        var isChecked = args.Value switch
        {
            bool value => value,
            string value when bool.TryParse(value, out var parsedValue) => parsedValue,
            _ => false,
        };

        if (isChecked)
        {
            _ = selectedRepositoryNames.Add(repository);
        }
        else
        {
            _ = selectedRepositoryNames.Remove(repository);
        }
    }

    private bool TryValidateSelection()
    {
        if (selectedRepositoryNames.Count > 0)
        {
            validationMessage = null;
            return true;
        }

        validationMessage = "Select at least one repository before continuing.";
        return false;
    }

    private async Task CloseDialogAsync()
    {
        var result = new LabelOperationDialogResult(
            Content.Mode,
            Content.OriginalLabelName,
            model.LabelName.Trim(),
            model.NormalisedColour,
            model.Description.Trim(),
            selectedRepositoryNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());

        await Dialog.CloseAsync(result);
    }

    private bool CanSelectRepository(string repository)
    {
        return Content.SelectableRepositories.Contains(repository, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Represents form state and validation for label operation dialogs.</summary>
    private sealed class LabelOperationFormModel
    {
        [Required(ErrorMessage = "Label name is required.")]
        public string LabelName { get; set; } = string.Empty;

        [RegularExpression("^#?[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid six-character hexadecimal colour.")]
        public string Colour { get; set; } = "#ededed";

        public string Description { get; set; } = string.Empty;

        public string NormalisedColour
            => string.IsNullOrWhiteSpace(Colour)
                ? "ededed"
                : Colour.Trim().TrimStart('#').ToLowerInvariant();

        public string DisplayColour
            => $"#{NormalisedColour}";
    }

    private sealed record ColourOption(string Name, string Hex);
}
