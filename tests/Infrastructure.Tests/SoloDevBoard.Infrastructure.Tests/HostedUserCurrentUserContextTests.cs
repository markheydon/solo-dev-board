using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class HostedUserCurrentUserContextTests
{
    [Fact]
    public void OwnerLogin_ClaimExists_ReturnsClaimValue()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
            new Claim(HostedAuthClaimTypes.InstallationId, "123"),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var result = sut.OwnerLogin;

        // Assert
        Assert.Equal("markheydon", result);
    }

    [Fact]
    public void OwnerLogin_ClaimMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
            new Claim(HostedAuthClaimTypes.InstallationId, "123"),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var act = () => sut.OwnerLogin;

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetAccessToken_RequiredClaimsExistAndTokenNotExpired_ReturnsToken()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
            new Claim(HostedAuthClaimTypes.InstallationId, "123"),
            new Claim(HostedAuthClaimTypes.TokenExpiresAt, DateTimeOffset.UtcNow.AddMinutes(5).ToString("O")),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var result = sut.GetAccessToken();

        // Assert
        Assert.Equal("token", result);
    }

    [Fact]
    public void GetAccessToken_InstallationClaimMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var act = () => sut.GetAccessToken();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetAccessToken_ExpiryClaimInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
            new Claim(HostedAuthClaimTypes.InstallationId, "123"),
            new Claim(HostedAuthClaimTypes.TokenExpiresAt, "not-a-date"),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var act = () => sut.GetAccessToken();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetAccessToken_ExpiryClaimInPast_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(new[]
        {
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.AccessToken, "token"),
            new Claim(HostedAuthClaimTypes.InstallationId, "123"),
            new Claim(HostedAuthClaimTypes.TokenExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O")),
        });
        var options = CreateOptions();
        var sut = new HostedUserCurrentUserContext(httpContextAccessor, options);

        // Act
        var act = () => sut.GetAccessToken();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Hosted");
        var principal = new ClaimsPrincipal(identity);

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
            },
        };
    }

    private static IOptions<GitHubAuthOptions> CreateOptions()
    {
        return Options.Create(new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            HostedAccessTokenClaimType = HostedAuthClaimTypes.AccessToken,
            HostedInstallationIdClaimType = HostedAuthClaimTypes.InstallationId,
            HostedTokenExpiresAtClaimType = HostedAuthClaimTypes.TokenExpiresAt,
        });
    }
}
