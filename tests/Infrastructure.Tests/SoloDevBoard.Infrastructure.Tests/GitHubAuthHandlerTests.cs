using System.Net;
using Moq;
using SoloDevBoard.Application.Identity;

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
        using var handler = new GitHubAuthHandler(currentUserContextMock.Object)
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
        currentUserContextMock.Verify(context => context.GetAccessToken(), Times.Once);
    }

    [Fact]
    public async Task SendAsync_EmptyAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns(string.Empty);

        var terminalHandler = new TerminalHandler();
        using var handler = new GitHubAuthHandler(currentUserContextMock.Object)
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

    [Fact]
    public async Task SendAsync_WhitespaceAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns("   ");

        var terminalHandler = new TerminalHandler();
        using var handler = new GitHubAuthHandler(currentUserContextMock.Object)
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

    [Fact]
    public async Task SendAsync_NullAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(context => context.GetAccessToken()).Returns((string)null!);

        var terminalHandler = new TerminalHandler();
        using var handler = new GitHubAuthHandler(currentUserContextMock.Object)
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
