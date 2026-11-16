using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Web.Components.Features.OpenSource;

/// <summary>Dialog for editing the operator canonical FUNDING.yml standard.</summary>
public partial class CanonicalFundingStandardEditorDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string InitialContent { get; set; } = string.Empty;

    [Inject]
    public ICanonicalFundingStandardService StandardService { get; set; } = default!;

    private string content = string.Empty;
    private string? validationMessage;
    private bool isSaving;

    protected override void OnInitialized()
    {
        content = InitialContent;
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task SaveAsync()
    {
        validationMessage = null;
        isSaving = true;
        try
        {
            await StandardService.SaveAsync(new SaveCanonicalFundingStandardRequest(content), CancellationToken.None)
                .ConfigureAwait(false);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (ArgumentException ex)
        {
            validationMessage = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            validationMessage = ex.Message;
        }
        finally
        {
            isSaving = false;
        }
    }
}