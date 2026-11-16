using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Web.Components.Features.OpenSource;

/// <summary>Funding file hygiene table and toolbar for the Open Source page.</summary>
public partial class OpenSourceFundingHygiene : ComponentBase
{
    [Inject]
    public IOpenSourceFundingService FundingService { get; set; } = default!;

    [Inject]
    public IInventoryRunCommandService RunCommandService { get; set; } = default!;

    [Inject]
    public ICanonicalFundingStandardService StandardService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private OpenSourceFundingPageDto? fundingPage;
    private FundingSummaryCountsDto? summary;
    private bool isLoading = true;
    private bool isScanRequestInFlight;
    private string? searchTerm;
    private string? inlineAlert;
    private Severity alertSeverity = Severity.Info;

    private IReadOnlyList<RepositoryFundingRowDto> FilteredRows
    {
        get
        {
            if (fundingPage?.Rows is not { Count: > 0 } rows)
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return rows;
            }

            return rows
                .Where(row => row.RepositoryFullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            fundingPage = await FundingService.GetFundingHygienePageAsync(CancellationToken.None).ConfigureAwait(false);
            summary = await FundingService.GetFundingSummaryAsync(CancellationToken.None).ConfigureAwait(false);
            inlineAlert = null;
        }
        catch (Exception ex)
        {
            inlineAlert = $"Could not load funding hygiene: {ex.Message}";
            alertSeverity = Severity.Error;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ScanNowAsync()
    {
        isScanRequestInFlight = true;
        try
        {
            var result = await RunCommandService.RequestScanNowAsync(CancellationToken.None).ConfigureAwait(false);
            Snackbar.Add(result.Message, result.Kind switch
            {
                RequestInventoryRunResultKind.Accepted => Severity.Success,
                RequestInventoryRunResultKind.AlreadyUnderway => Severity.Info,
                _ => Severity.Warning,
            });
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Could not request a scan: {ex.Message}", Severity.Error);
        }
        finally
        {
            isScanRequestInFlight = false;
        }
    }

    private async Task OpenStandardEditor()
    {
        var parameters = new DialogParameters<CanonicalFundingStandardEditorDialog>
        {
            { dialog => dialog.InitialContent, fundingPage?.Standard?.Content ?? string.Empty },
        };

        var dialog = await DialogService.ShowAsync<CanonicalFundingStandardEditorDialog>(
            "Canonical funding file",
            parameters).ConfigureAwait(false);
        var dialogResult = await dialog.Result.ConfigureAwait(false);
        if (dialogResult is { Canceled: false })
        {
            await LoadAsync().ConfigureAwait(false);
        }
    }

    private static string FormatTrigger(InventoryRunTriggerDto trigger) =>
        trigger == InventoryRunTriggerDto.Scheduled ? "Scheduled" : "Manual";

    private static string FormatOutcome(InventoryRunOutcomeDto outcome) => outcome switch
    {
        InventoryRunOutcomeDto.Completed => "Completed",
        InventoryRunOutcomeDto.Partial => "Partial",
        InventoryRunOutcomeDto.Failed => "Failed",
        _ => outcome.ToString(),
    };

    private static string FormatAccess(InventoryAccessKindDto access) =>
        access == InventoryAccessKindDto.Installation ? "Installation" : "Personal access token";
}