using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_ValidAccessToken_AddsBearerAuthorisationHeader()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns("test-token");

        var terminalHandler = new TerminalHandler();
        using var handler = CreateHandler(currentUserContextMock.Object);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(terminalHandler.LastRequest);
        Assert.NotNull(terminalHandler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", terminalHandler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-token", terminalHandler.LastRequest.Headers.Authorization.Parameter);
        currentUserContextMock.Verify(context => context.GetAccessToken(), Times.Once);
    }

    [Fact]
    public async Task SendAsync_EmptyAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns(string.Empty);

        var terminalHandler = new TerminalHandler();
        using var handler = CreateHandler(currentUserContextMock.Object);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        var act = async () => _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task SendAsync_WhitespaceAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns("   ");

        var terminalHandler = new TerminalHandler();
        using var handler = CreateHandler(currentUserContextMock.Object);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        var act = async () => _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task SendAsync_NullAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns((string)null!);

        var terminalHandler = new TerminalHandler();
        using var handler = CreateHandler(currentUserContextMock.Object);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        var act = async () => _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task SendAsync_HostedModeUnauthorizedResponse_ThrowsHostedAuthenticationRequiredException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns("revoked-token");

        var terminalHandler = new TerminalHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(currentUserContextMock.Object, hostedSignInEnabled: true);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        var act = async () => _ = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HostedAuthenticationRequiredException>(act);
    }

    [Fact]
    public async Task SendAsync_PatModeUnauthorizedResponse_ReturnsUnauthorizedResponse()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns("pat-token");

        var terminalHandler = new TerminalHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(currentUserContextMock.Object, hostedSignInEnabled: false);
        handler.InnerHandler = terminalHandler;

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        // Act
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static GitHubAuthHandler CreateHandler(ICurrentUserContext currentUserContext, bool hostedSignInEnabled = false) =>
        new(
            currentUserContext,
            Options.Create(new GitHubAuthOptions { HostedSignInEnabled = hostedSignInEnabled }));

    private sealed class TerminalHandler(HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
