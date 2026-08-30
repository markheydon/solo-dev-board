using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.App.Tests;

public sealed class HostedAuthenticationRecoveryRouteTests
{
    [Fact]
    public void BuildErrorUrl_SessionExpiredWithReturnUrl_IncludesReasonAndReturnUrl()
    {
        // Act
        var url = HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SessionExpired, "/repositories");

        // Assert
        Assert.Contains("reason=session-expired", url, StringComparison.Ordinal);
        Assert.Contains("returnUrl=%2Frepositories", url, StringComparison.Ordinal);
    }
}
