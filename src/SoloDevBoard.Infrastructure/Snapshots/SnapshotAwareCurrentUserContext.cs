using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Delegates to captured snapshot credentials when present.</summary>
public sealed class SnapshotAwareCurrentUserContext(
    SnapshotCollectionCredentialSlot credentialSlot,
    ICurrentUserContext inner) : ICurrentUserContext
{
    private readonly SnapshotCollectionCredentialSlot _credentialSlot = credentialSlot ?? throw new ArgumentNullException(nameof(credentialSlot));
    private readonly ICurrentUserContext _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc/>
    public string OwnerLogin =>
        SnapshotCollectionCredentialAmbient.TryGet(out var ambientLogin, out _)
            ? ambientLogin
            : _credentialSlot.HasCapture
                ? _credentialSlot.OwnerLogin
                : _inner.OwnerLogin;

    /// <inheritdoc/>
    public string GetAccessToken() =>
        SnapshotCollectionCredentialAmbient.TryGet(out _, out var ambientToken)
            ? ambientToken
            : _credentialSlot.HasCapture
                ? _credentialSlot.AccessToken
                : _inner.GetAccessToken();
}
