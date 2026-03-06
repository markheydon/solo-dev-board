using System.Net;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_ValidPersonalAccessToken_AddsBearerAuthorisationHeader()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            PersonalAccessToken = "test-token"
        });

        var terminalHandler = new TerminalHandler();
        using var handler = new GitHubAuthHandler(options)
        {
            InnerHandler = terminalHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(terminalHandler.LastRequest);
        Assert.NotNull(terminalHandler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", terminalHandler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-token", terminalHandler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_EmptyPersonalAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            PersonalAccessToken = string.Empty
        });

        var terminalHandler = new TerminalHandler();
        using var handler = new GitHubAuthHandler(options)
        {
            InnerHandler = terminalHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        var act = async () => _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private sealed class TerminalHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
