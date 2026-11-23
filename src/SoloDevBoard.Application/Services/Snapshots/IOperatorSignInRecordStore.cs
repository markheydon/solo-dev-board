namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Persistence for hosted sign-in records under <c>schedule/sign-ins/</c>.</summary>
public interface IOperatorSignInRecordStore
{
    /// <summary>Creates or replaces the sign-in record.</summary>
    Task UpsertAsync(OperatorSignInRecord record, CancellationToken cancellationToken = default);

    /// <summary>Lists all sign-in records.</summary>
    Task<IReadOnlyList<OperatorSignInRecord>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one record by sanitised login.</summary>
    Task<OperatorSignInRecord?> GetByLoginAsync(string sanitisedLogin, CancellationToken cancellationToken = default);

    /// <summary>Conditionally updates schedule hold.</summary>
    Task<bool> TryUpdateScheduleHoldAsync(
        string sanitisedLogin,
        bool scheduleHold,
        string ifMatchEtag,
        CancellationToken cancellationToken = default);

    /// <summary>Conditionally updates protected refresh token fields only.</summary>
    Task<bool> TryUpdateProtectedRefreshTokenAsync(
        string sanitisedLogin,
        string protectedRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string ifMatchEtag,
        CancellationToken cancellationToken = default);
}
