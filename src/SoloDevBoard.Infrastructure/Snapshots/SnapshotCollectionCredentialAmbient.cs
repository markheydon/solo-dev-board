namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>
/// Flow-local credentials for background snapshot walks. Used when named <see cref="HttpClient"/>
/// handlers are resolved outside the credential DI scope (for example the Functions worker).
/// </summary>
internal static class SnapshotCollectionCredentialAmbient
{
    private static readonly AsyncLocal<AmbientCapture?> Current = new();

    public static bool TryGet(out string ownerLogin, out string accessToken)
    {
        var capture = Current.Value;
        if (capture is null)
        {
            ownerLogin = string.Empty;
            accessToken = string.Empty;
            return false;
        }

        ownerLogin = capture.OwnerLogin;
        accessToken = capture.AccessToken;
        return true;
    }

    public static void Set(string ownerLogin, string accessToken)
    {
        Current.Value = new AmbientCapture(ownerLogin, accessToken);
    }

    public static void Clear() => Current.Value = null;

    private sealed record AmbientCapture(string OwnerLogin, string AccessToken);
}