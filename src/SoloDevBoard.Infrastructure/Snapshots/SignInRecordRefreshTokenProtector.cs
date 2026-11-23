using Microsoft.AspNetCore.DataProtection;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class SignInRecordRefreshTokenProtector(IDataProtectionProvider provider) : ISignInRecordRefreshTokenProtector
{
    private readonly IDataProtector _protector = (provider ?? throw new ArgumentNullException(nameof(provider)))
        .CreateProtector("SoloDevBoard.Schedule.SignInRecord.RefreshToken");

    /// <inheritdoc/>
    public string Protect(string refreshToken)
        => _protector.Protect(refreshToken);

    /// <inheritdoc/>
    public string Unprotect(string protectedRefreshToken)
        => _protector.Unprotect(protectedRefreshToken);
}
