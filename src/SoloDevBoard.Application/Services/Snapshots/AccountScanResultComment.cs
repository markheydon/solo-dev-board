namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Short result comments stored on the account scan status blob.</summary>
public static class AccountScanResultComment
{
    /// <summary>Comment for a failed scan, from the snapshot run reason.</summary>
    public static string ForFailure(OperatorSnapshotRunReason? reason, string? detail)
    {
        var text = reason switch
        {
            OperatorSnapshotRunReason.SignInOrToken => "Could not reach GitHub with the stored sign-in.",
            OperatorSnapshotRunReason.RateLimit => "GitHub rate limit reached.",
            _ => "Something went wrong.",
        };

        return string.IsNullOrWhiteSpace(detail) || reason == OperatorSnapshotRunReason.SignInOrToken
            ? text
            : $"{text} {detail.Trim()}";
    }
}
