namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Protector stub for lean posture.</summary>
public sealed class NoOpSignInRecordRefreshTokenProtector : ISignInRecordRefreshTokenProtector
{
    public string Protect(string refreshToken) => refreshToken;

    public string Unprotect(string protectedRefreshToken) => protectedRefreshToken;
}
