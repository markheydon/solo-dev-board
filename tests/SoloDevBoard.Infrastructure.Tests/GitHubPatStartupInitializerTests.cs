using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubPatStartupInitializer"/>.</summary>
public sealed class GitHubPatStartupInitializerTests
{
    [Fact]
    public async Task StartAsync_OwnerLoginConfigured_StillProbesPatAndStoresResolvedLogin()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"login":"resolved-user"}"""),
            }));

        var factory = new StubHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") });
        var resolver = new GitHubPatOwnerLoginResolver(
            factory,
            new TestAppVersionService(),
            NullLogger<GitHubPatOwnerLoginResolver>.Instance);

        var resolvedPatOwnerLogin = new ResolvedPatOwnerLogin();
        var initializer = new GitHubPatStartupInitializer(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                OwnerLogin = "configured-user",
                PersonalAccessToken = "ghp_test",
            }),
            resolvedPatOwnerLogin,
            resolver,
            NullLogger<GitHubPatStartupInitializer>.Instance);

        await initializer.StartAsync(cancellationToken);

        Assert.Equal("resolved-user", resolvedPatOwnerLogin.Value);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task StartAsync_E2ePlaceholderPat_DoesNotProbePat()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"login":"resolved-user"}"""),
            }));

        var factory = new StubHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") });
        var resolver = new GitHubPatOwnerLoginResolver(
            factory,
            new TestAppVersionService(),
            NullLogger<GitHubPatOwnerLoginResolver>.Instance);

        var initializer = new GitHubPatStartupInitializer(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = false,
                OwnerLogin = "ci-test-user",
                PersonalAccessToken = AuthConfigurationPlaceholders.CiE2ePlaceholder,
            }),
            new ResolvedPatOwnerLogin(),
            resolver,
            NullLogger<GitHubPatStartupInitializer>.Instance);

        await initializer.StartAsync(cancellationToken);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task StartAsync_HostedSignInEnabled_DoesNotProbePat()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"login":"resolved-user"}"""),
            }));

        var factory = new StubHttpClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") });
        var resolver = new GitHubPatOwnerLoginResolver(
            factory,
            new TestAppVersionService(),
            NullLogger<GitHubPatOwnerLoginResolver>.Instance);

        var initializer = new GitHubPatStartupInitializer(
            Options.Create(new GitHubAuthOptions
            {
                HostedSignInEnabled = true,
                PersonalAccessToken = "ghp_test",
            }),
            new ResolvedPatOwnerLogin(),
            resolver,
            NullLogger<GitHubPatStartupInitializer>.Instance);

        await initializer.StartAsync(cancellationToken);

        Assert.Empty(handler.Requests);
    }

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return handler(request, cancellationToken);
        }
    }
}
