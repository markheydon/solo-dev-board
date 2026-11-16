namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Scan presentation state for funding table rows (includes synthetic catalogue rows).</summary>
public enum RowScanState
{
    Checked,
    NotScanned,
    NotYetScanned,
    NotVisible,
}