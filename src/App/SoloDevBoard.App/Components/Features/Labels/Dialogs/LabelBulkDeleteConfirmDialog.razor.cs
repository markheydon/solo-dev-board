using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SoloDevBoard.App.Components.Features.Labels.Dialogs;

/// <summary>Provides the code-behind for the bulk label delete confirmation dialog.</summary>
public partial class LabelBulkDeleteConfirmDialog
{
    /// <summary>Gets or sets the dialog request payload.</summary>
    [Parameter]
    public LabelBulkDeleteConfirmDialogRequest Content { get; set; } = new([], []);

    /// <summary>Gets or sets the active MudBlazor dialog instance.</summary>
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    private Task CancelAsync()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private Task ConfirmAsync()
    {
        MudDialog.Close(DialogResult.Ok(true));
        return Task.CompletedTask;
    }
}
