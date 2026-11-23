namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Captured credentials for a background snapshot walk in this scope.</summary>
public sealed class SnapshotCollectionCredentialSlot
{
    /// <summary>Gets whether this scope has captured credentials.</summary>
    public bool HasCapture { get; private set; }

    /// <summary>Gets the captured owner login.</summary>
    public string OwnerLogin { get; private set; } = string.Empty;

    /// <summary>Gets the captured access token.</summary>
    public string AccessToken { get; private set; } = string.Empty;

    /// <summary>Sets credentials for this scope.</summary>
    public void Set(string ownerLogin, string accessToken)
    {
        OwnerLogin = ownerLogin;
        AccessToken = accessToken;
        HasCapture = true;
    }

    /// <summary>Clears captured credentials.</summary>
    public void Clear()
    {
        OwnerLogin = string.Empty;
        AccessToken = string.Empty;
        HasCapture = false;
    }
}
