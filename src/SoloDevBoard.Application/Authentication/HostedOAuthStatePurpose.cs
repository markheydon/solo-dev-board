namespace SoloDevBoard.Application.Authentication;

/// <summary>OAuth <c>state</c> cookie purpose for the hosted GitHub App callback.</summary>
public static class HostedOAuthStatePurpose
{
    /// <summary>First authorisation of hosted sign-in (browser cookie).</summary>
    public const string Browser = "browser";

    /// <summary>Second authorisation during the same sign-in (worker token).</summary>
    public const string WorkerDuringSignIn = "worker-during-sign-in";

    /// <summary>Restore the browser refresh token after a rejected worker leg.</summary>
    public const string BrowserRestore = "browser-restore";

    /// <summary>Operator-initiated worker token refresh from the Repositories page.</summary>
    public const string WorkerRefresh = "worker-refresh";

    /// <summary>Returns whether <paramref name="purpose"/> is a known value.</summary>
    public static bool IsKnown(string? purpose) =>
        purpose is Browser or WorkerDuringSignIn or BrowserRestore or WorkerRefresh;
}
