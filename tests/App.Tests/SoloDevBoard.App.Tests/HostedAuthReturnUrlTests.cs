using SoloDevBoard.App.Authentication;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="HostedAuthReturnUrl"/>.</summary>
public sealed class HostedAuthReturnUrlTests
{
    [Theory]
    [InlineData("/welcome?ReturnUrl=%2Fabout", "/about")]
    [InlineData("/welcome?returnUrl=%2Frepositories", "/repositories")]
    public void GetRequestedReturnUrl_QueryContainsReturnUrl_ReturnsSafePath(string requestPath, string expectedReturnUrl)
    {
        var returnUrl = HostedAuthReturnUrl.GetRequestedReturnUrl(new Uri($"https://localhost{requestPath}", UriKind.Absolute));

        Assert.Equal(expectedReturnUrl, returnUrl);
    }

    [Fact]
    public void BuildSignInUrl_ReturnUrlProvided_IncludesReturnUrlQueryParameter()
    {
        var signInUrl = HostedAuthReturnUrl.BuildSignInUrl("/about");

        Assert.Equal("/auth/sign-in?returnUrl=%2Fabout", signInUrl);
    }

    [Fact]
    public void ResolveDestination_ReturnUrlMissing_UsesHomeRoute()
    {
        Assert.Equal("/", HostedAuthReturnUrl.ResolveDestination(null));
    }
}
