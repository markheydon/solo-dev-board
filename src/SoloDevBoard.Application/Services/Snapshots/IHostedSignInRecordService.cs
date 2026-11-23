namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Records completed hosted sign-in for daily scheduling.</summary>
public interface IHostedSignInRecordService
{
    /// <summary>Upserts a sign-in record and clears schedule hold.</summary>
    Task RecordCompletedSignInAsync(
        string ownerLogin,
        IReadOnlyList<string> organisationLogins,
        string refreshToken,
        DateTimeOffset? refreshTokenExpiresAtUtc,
        CancellationToken cancellationToken = default);
}
