using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="GitHubAuthenticationSummaryService"/>.</summary>
public sealed class GitHubAuthenticationSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_HostedSignInEnabled_ReturnsSignedInHostedSummary()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var authenticationStateProvider = new TestAuthenticationStateProvider(
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                [
                    new System.Security.Claims.Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
                ],
                authenticationType: "test")));

        var service = new GitHubAuthenticationSummaryService(
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }),
            authenticationStateProvider,
            Substitute.For<IGitHubConnectivityStatusService>());

        var summary = await service.GetSummaryAsync(cancellationToken);

        Assert.Equal("Hosted sign-in", summary.ModeLabel);
        Assert.Equal("Signed in as", summary.IdentityLabel);
        Assert.Equal("markheydon", summary.GitHubLogin);
    }

    [Fact]
    public async Task GetSummaryAsync_PatMode_ReturnsConnectedPatSummary()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var connectivityStatusService = Substitute.For<IGitHubConnectivityStatusService>();
        connectivityStatusService
            .GetStatusAsync(cancellationToken)
            .Returns(new GitHubConnectivityStatusDto(true, "markheydon", "Connected as @markheydon."));

        var service = new GitHubAuthenticationSummaryService(
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = false }),
            new TestAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal()),
            connectivityStatusService);

        var summary = await service.GetSummaryAsync(cancellationToken);

        Assert.Equal("PAT-only local trusted mode", summary.ModeLabel);
        Assert.Equal("Connected as", summary.IdentityLabel);
        Assert.Equal("markheydon", summary.GitHubLogin);
    }

    private sealed class TestAuthenticationStateProvider(System.Security.Claims.ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
