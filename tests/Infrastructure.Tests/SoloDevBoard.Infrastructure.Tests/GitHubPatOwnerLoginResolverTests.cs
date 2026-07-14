using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubPatOwnerLoginResolverTests
{
    [Fact]
    public async Task ResolveAsync_ValidPat_ReturnsAuthenticatedLogin()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"login":"markheydon"}"""),
            }));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var factory = new StubHttpClientFactory(httpClient);
        var sut = new GitHubPatOwnerLoginResolver(factory, new TestAppVersionService(), NullLogger<GitHubPatOwnerLoginResolver>.Instance);

        // Act
        var result = await sut.ResolveAsync("ghp_test-token");

        // Assert
        Assert.Equal("markheydon", result);
        var request = handler.Requests[0];
        Assert.Equal(new Uri("https://api.github.com/user"), request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal("ghp_test-token", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ResolveAsync_InvalidPat_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"message":"Bad credentials"}"""),
            }));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var factory = new StubHttpClientFactory(httpClient);
        var sut = new GitHubPatOwnerLoginResolver(factory, new TestAppVersionService(), NullLogger<GitHubPatOwnerLoginResolver>.Instance);

        // Act
        var act = () => sut.ResolveAsync("invalid-token");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
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
