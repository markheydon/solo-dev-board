namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Suspension reason returned to the page.</summary>
public enum AccountScanSuspensionReason
{
    /// <summary>No suspension reason.</summary>
    None,

    /// <summary>Admission lists block scheduled collection.</summary>
    NotAllowed,
}
