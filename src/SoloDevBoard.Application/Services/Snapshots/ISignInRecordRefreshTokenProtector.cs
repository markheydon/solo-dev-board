namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Protects refresh tokens before blob persistence.</summary>
public interface ISignInRecordRefreshTokenProtector
{
    /// <summary>Protects a plaintext refresh token.</summary>
    string Protect(string refreshToken);

    /// <summary>Unprotects stored ciphertext.</summary>
    string Unprotect(string protectedRefreshToken);
}
