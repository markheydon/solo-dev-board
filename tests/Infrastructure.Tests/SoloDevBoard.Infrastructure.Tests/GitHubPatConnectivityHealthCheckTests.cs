using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubPatConnectivityHealthCheck"/>.</summary>
public sealed class GitHubPatConnectivityHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ValidPat_ReturnsHealthy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"login":"solo-dev"}"""),
            }));

        var resolver = CreateResolver(handler);
        var healthCheck = new GitHubPatConnectivityHealthCheck(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = "ghp_test",
            }),
            resolver);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("solo-dev", result.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckHealthAsync_E2ePlaceholderPat_ReturnsHealthyWithoutProbe()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var resolver = CreateResolver(handler);
        var healthCheck = new GitHubPatConnectivityHealthCheck(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = AuthConfigurationPlaceholders.CiE2ePlaceholder,
            }),
            resolver);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("E2E placeholder", result.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckHealthAsync_HostedSignInEnabled_ReturnsHealthyWithoutProbe()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var resolver = CreateResolver(handler);
        var healthCheck = new GitHubPatConnectivityHealthCheck(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = true,
                PersonalAccessToken = "ghp_test",
            }),
            resolver);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Hosted sign-in", result.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckHealthAsync_InvalidPat_ReturnsUnhealthy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"message":"Bad credentials"}"""),
            }));

        var resolver = CreateResolver(handler);
        var healthCheck = new GitHubPatConnectivityHealthCheck(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                PersonalAccessToken = "ghp_invalid",
            }),
            resolver);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static GitHubPatOwnerLoginResolver CreateResolver(StubHttpMessageHandler handler) =>
        new(
            new StubHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") }),
            new TestAppVersionService(),
            NullLogger<GitHubPatOwnerLoginResolver>.Instance);

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
