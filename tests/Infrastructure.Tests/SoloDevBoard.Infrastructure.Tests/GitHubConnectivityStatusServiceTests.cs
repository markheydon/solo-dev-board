using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubConnectivityStatusService"/>.</summary>
public sealed class GitHubConnectivityStatusServiceTests
{
    [Fact]
    public async Task GetStatusAsync_HostedSignInEnabled_ReturnsNotApplicableState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var currentUserContext = Substitute.For<ICurrentUserContext>();
        var service = new GitHubConnectivityStatusService(
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }),
            currentUserContext);

        var status = await service.GetStatusAsync(cancellationToken);

        Assert.False(status.IsConnected);
        Assert.Null(status.OwnerLogin);
        Assert.Contains("Hosted sign-in is enabled", status.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStatusAsync_PatNotConfigured_ReturnsDisconnectedState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var currentUserContext = Substitute.For<ICurrentUserContext>();
        var service = new GitHubConnectivityStatusService(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = string.Empty,
            }),
            currentUserContext);

        var status = await service.GetStatusAsync(cancellationToken);

        Assert.False(status.IsConnected);
        Assert.Null(status.OwnerLogin);
        Assert.Contains("not configured", status.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusAsync_PatConnected_ReturnsConnectedState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var currentUserContext = Substitute.For<ICurrentUserContext>();
        currentUserContext.OwnerLogin.Returns("solo-dev");

        var service = new GitHubConnectivityStatusService(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = "ghp_test",
            }),
            currentUserContext);

        var status = await service.GetStatusAsync(cancellationToken);

        Assert.True(status.IsConnected);
        Assert.Equal("solo-dev", status.OwnerLogin);
        Assert.Contains("@solo-dev", status.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStatusAsync_OwnerLoginUnavailable_ReturnsDisconnectedState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var currentUserContext = Substitute.For<ICurrentUserContext>();
        currentUserContext
            .When(context => _ = context.OwnerLogin)
            .Throw(new InvalidOperationException("Owner login is not available."));

        var service = new GitHubConnectivityStatusService(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = "ghp_test",
            }),
            currentUserContext);

        var status = await service.GetStatusAsync(cancellationToken);

        Assert.False(status.IsConnected);
        Assert.Null(status.OwnerLogin);
        Assert.Contains("not connected", status.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
