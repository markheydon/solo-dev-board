using System.Security.Claims;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class HostedTokenExpiryValidatorTests
{
    [Fact]
    public void IsExpired_ExpiryClaimInPast_ReturnsTrue()
    {
        // Arrange
        var principal = CreatePrincipal(HostedAuthClaimTypes.TokenExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"));
        var options = CreateOptions();

        // Act
        var result = HostedTokenExpiryValidator.IsExpired(principal, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_ExpiryClaimInFuture_ReturnsFalse()
    {
        // Arrange
        var principal = CreatePrincipal(HostedAuthClaimTypes.TokenExpiresAt, DateTimeOffset.UtcNow.AddMinutes(5).ToString("O"));
        var options = CreateOptions();

        // Act
        var result = HostedTokenExpiryValidator.IsExpired(principal, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_ExpiryClaimMissing_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon")], "Hosted"));
        var options = CreateOptions();

        // Act
        var result = HostedTokenExpiryValidator.IsExpired(principal, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_ExpiryClaimTypeNotConfigured_ReturnsFalse()
    {
        // Arrange
        var principal = CreatePrincipal(HostedAuthClaimTypes.TokenExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"));
        var options = CreateOptions();
        options.HostedTokenExpiresAtClaimType = string.Empty;

        // Act
        var result = HostedTokenExpiryValidator.IsExpired(principal, options);

        // Assert
        Assert.False(result);
    }

    private static ClaimsPrincipal CreatePrincipal(string claimType, string claimValue)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, claimValue)], "Hosted"));
    }

    private static GitHubAuthOptions CreateOptions() =>
        new()
        {
            HostedTokenExpiresAtClaimType = HostedAuthClaimTypes.TokenExpiresAt,
        };
}
