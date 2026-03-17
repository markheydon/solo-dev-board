using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace SoloDevBoard.App.Components.Features.Labels.Dialogs;

/// <summary>Provides the code-behind for the label operation dialog component.</summary>
public partial class LabelOperationDialog
{
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

    /// <summary>Gets or sets the active MudBlazor dialog instance.</summary>
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    private readonly LabelOperationFormModel model = new();
    private HashSet<string> selectableRepositoryNames = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> selectedRepositoryNames = new(StringComparer.OrdinalIgnoreCase);
    private EditContext editContext = default!;
    private string? validationMessage;
    private string DialogTitle => Content.Mode switch
    {
        LabelOperationMode.Create => "New label",
        LabelOperationMode.Edit => "Edit label",
        _ => "Delete label",
    };

    private string SubmitButtonText => Content.Mode switch
    {
        LabelOperationMode.Create => "Create",
        LabelOperationMode.Edit => "Save",
        _ => "Delete label",
    };

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectableRepositoryNames = Content.SelectableRepositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectableRepositoryNames.Count == 0 && Content.Mode == LabelOperationMode.Create)
        {
            selectableRepositoryNames = Content.AvailableRepositories
                .Where(repository => !string.IsNullOrWhiteSpace(repository))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        model.LabelName = Content.LabelName;
        model.Colour = string.IsNullOrWhiteSpace(Content.Colour)
            ? "#ededed"
            : Content.Colour.StartsWith('#')
                ? Content.Colour
                : $"#{Content.Colour}";
        model.Description = Content.Description;

        selectedRepositoryNames = Content.SelectedRepositories
            .Where(repository => selectableRepositoryNames.Contains(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedRepositoryNames.Count == 0)
        {
            selectedRepositoryNames = new HashSet<string>(selectableRepositoryNames, StringComparer.OrdinalIgnoreCase);
        }

        editContext = new EditContext(model);
        validationMessage = null;
    }

    private async Task ConfirmAsync()
    {
        if (Content.Mode != LabelOperationMode.Delete && !editContext.Validate())
        {
            return;
        }

        if (!TryValidateSelection())
        {
            return;
        }

        await CloseDialogAsync();
    }

    private Task CancelAsync()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private void OnRepositoryToggleChanged(string repository, bool isChecked)
    {
        if (!CanSelectRepository(repository))
        {
            return;
        }

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

    private Task CloseDialogAsync()
    {
        var result = new LabelOperationDialogResult(
            Content.Mode,
            Content.OriginalLabelName,
            model.LabelName.Trim(),
            model.NormalisedColour,
            model.Description.Trim(),
            selectedRepositoryNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());

        MudDialog.Close(DialogResult.Ok(result));
        return Task.CompletedTask;
    }

    private bool CanSelectRepository(string repository)
    {
        return selectableRepositoryNames.Contains(repository);
    }

    private string GetColourPickerIconStyle()
    {
        return $"color: {ResolveDisplayColour(model.Colour)};";
    }

    private static string ResolveDisplayColour(string? colour)
    {
        var candidate = colour?.Trim().TrimStart('#') ?? string.Empty;

        if (candidate.Length == 8)
        {
            candidate = candidate[..6];
        }

        return candidate.Length == 6 && candidate.All(Uri.IsHexDigit)
            ? $"#{candidate}"
            : "#ededed";
    }

    /// <summary>Represents form state and validation for label operation dialogs.</summary>
    private sealed class LabelOperationFormModel
    {
        private string colour = "#ededed";

        [Required(ErrorMessage = "Label name is required.")]
        public string LabelName { get; set; } = string.Empty;

        [RegularExpression("^#?[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$", ErrorMessage = "Use a valid six- or eight-character hexadecimal colour.")]
        public string Colour
        {
            get => colour;
            set => colour = value;
        }

        public string Description { get; set; } = string.Empty;

        public string NormalisedColour
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Colour))
                {
                    return "ededed";
                }

                var hex = Colour.Trim().TrimStart('#').ToLowerInvariant();

                // MudColorPicker may emit 8-digit hex with alpha; labels API expects 6-digit RGB.
                if (hex.Length == 8)
                {
                    hex = hex[..6];
                }

                return hex;
            }
        }

        public string DisplayColour
            => $"#{NormalisedColour}";
    }
}
