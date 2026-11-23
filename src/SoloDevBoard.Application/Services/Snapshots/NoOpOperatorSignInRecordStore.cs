namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Lean posture stub that never persists sign-in records.</summary>
public sealed class NoOpOperatorSignInRecordStore : IOperatorSignInRecordStore
{
    public Task UpsertAsync(OperatorSignInRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<OperatorSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OperatorSignInRecord>>([]);

    public Task<OperatorSignInRecord?> GetByLoginAsync(string sanitisedLogin, CancellationToken cancellationToken = default)
        => Task.FromResult<OperatorSignInRecord?>(null);

    public Task<bool> TryUpdateScheduleHoldAsync(string sanitisedLogin, bool scheduleHold, string ifMatchEtag, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> TryUpdateProtectedRefreshTokenAsync(
        string sanitisedLogin,
        string protectedRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string ifMatchEtag,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
