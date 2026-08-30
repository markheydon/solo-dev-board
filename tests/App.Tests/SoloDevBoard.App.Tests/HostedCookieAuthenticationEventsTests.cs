using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Tests;

public sealed class HostedCookieAuthenticationEventsTests
{
    private const string SessionExpiredItemKey = "solo-dev-board.hosted-session-expired";

    [Fact]
    public async Task ValidatePrincipal_AccessTokenExpiryClaimMissing_DoesNotRefreshOrReject()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>());
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(options, accessTokenExpiresAtUtc: null, refreshToken: "refresh-token-123");
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.False(context.ShouldRenew);
        Assert.Equal("access-token-123", context.Principal!.FindFirstValue(HostedAuthClaimTypes.AccessToken));
    }

    [Fact]
    public async Task ValidatePrincipal_AccessTokenAlreadyExpired_RefreshSucceeds()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "new-access-token", refresh_token = "new-refresh-token", expires_in = 3600, refresh_token_expires_in = 604800 }),
        ]));
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(
            options,
            accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10),
            refreshToken: "refresh-token-123",
            refreshTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(6));
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.True(context.ShouldRenew);
        Assert.Equal("new-access-token", context.Principal!.FindFirstValue(HostedAuthClaimTypes.AccessToken));
    }

    [Fact]
    public async Task ValidatePrincipal_AccessTokenNotExpired_DoesNotRefresh()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>());
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(options, accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1));
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.False(context.ShouldRenew);
        Assert.Equal("access-token-123", context.Principal!.FindFirstValue(HostedAuthClaimTypes.AccessToken));
    }

    [Fact]
    public async Task ValidatePrincipal_AccessTokenExpired_RefreshSucceeds_RefreshesAndReplacesPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "new-access-token", refresh_token = "new-refresh-token", expires_in = 3600, refresh_token_expires_in = 604800 }),
        ]));
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(
            options,
            accessToken: "access-token-123",
            accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(2),
            refreshToken: "refresh-token-123",
            refreshTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(6));
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.True(context.ShouldRenew);
        Assert.Equal("new-access-token", context.Principal!.FindFirstValue(HostedAuthClaimTypes.AccessToken));
        Assert.Equal("new-refresh-token", context.Principal!.FindFirstValue(HostedAuthClaimTypes.RefreshToken));
    }

    [Fact]
    public async Task ValidatePrincipal_RefreshResponseOmitsRefreshToken_ReusesExistingRefreshToken()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "new-access-token", expires_in = 3600 }),
        ]));
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(
            options,
            accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(2),
            refreshToken: "refresh-token-123");
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.True(context.ShouldRenew);
        Assert.Equal("new-access-token", context.Principal!.FindFirstValue(HostedAuthClaimTypes.AccessToken));
        Assert.Equal("refresh-token-123", context.Principal!.FindFirstValue(HostedAuthClaimTypes.RefreshToken));
    }

    [Fact]
    public async Task ValidatePrincipal_MissingRefreshToken_RejectsPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>());
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(options, accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(2));
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.False(context.ShouldRenew);
        Assert.Null(context.Principal);
        Assert.True(httpContext.Items.TryGetValue(SessionExpiredItemKey, out var value) && value is true);
    }

    [Fact]
    public async Task ValidatePrincipal_RefreshTokenExpired_RejectsPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>());
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(
            options,
            accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(2),
            refreshToken: "refresh-token-123",
            refreshTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidatePrincipal_RefreshFails_RejectsPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var gateway = CreateGateway(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") },
        ]));
        var httpContext = CreateHttpContext(gateway);
        var principal = CreatePrincipal(
            options,
            accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(2),
            refreshToken: "refresh-token-123");
        var context = CreateValidateContext(httpContext, principal);
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnValidatePrincipal(context);

        // Assert
        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task OnRedirectToLogin_SessionExpiredFlag_RedirectsToSessionExpiredErrorPage()
    {
        // Arrange
        var options = CreateOptions();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[SessionExpiredItemKey] = true;
        var redirectContext = CreateRedirectContext(httpContext, "/Account/Login?ReturnUrl=%2Fdashboard");
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.StartsWith("/auth/error?reason=session-expired", redirectContext.RedirectUri);
        Assert.Contains("returnUrl=%2Fdashboard", redirectContext.RedirectUri);
        Assert.Equal(StatusCodes.Status302Found, redirectContext.Response.StatusCode);
        Assert.Equal(redirectContext.RedirectUri, redirectContext.Response.Headers["Location"].ToString());
    }

    [Fact]
    public async Task OnRedirectToLogin_NoSessionExpiredFlag_PreservesOriginalRedirectUri()
    {
        // Arrange
        var options = CreateOptions();
        var httpContext = new DefaultHttpContext();
        var redirectContext = CreateRedirectContext(httpContext, "/Account/Login?ReturnUrl=%2Fdashboard");
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.Equal("/Account/Login?ReturnUrl=%2Fdashboard", redirectContext.RedirectUri);
        Assert.Equal(StatusCodes.Status302Found, redirectContext.Response.StatusCode);
        Assert.Equal(redirectContext.RedirectUri, redirectContext.Response.Headers["Location"].ToString());
    }

    [Fact]
    public async Task OnRedirectToLogin_ApiEndpoint_Returns401WithLocationHeader()
    {
        // Arrange
        var options = CreateOptions();
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new DisableCookieRedirectTestMetadata()),
            "api"));
        var redirectContext = CreateRedirectContext(httpContext, "/Account/Login?ReturnUrl=%2Fdashboard");
        var events = HostedCookieAuthenticationEvents.Create(options);

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, redirectContext.Response.StatusCode);
        Assert.Equal(redirectContext.RedirectUri, redirectContext.Response.Headers["Location"].ToString());
    }

    private sealed class DisableCookieRedirectTestMetadata : IDisableCookieRedirectMetadata
    {
    }

    private static GitHubAuthOptions CreateOptions()
    {
        return new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
            HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            HostedAccessTokenClaimType = HostedAuthClaimTypes.AccessToken,
            HostedInstallationIdClaimType = HostedAuthClaimTypes.InstallationId,
            HostedTokenExpiresAtClaimType = HostedAuthClaimTypes.TokenExpiresAt,
            HostedRefreshTokenClaimType = HostedAuthClaimTypes.RefreshToken,
            HostedRefreshTokenExpiresAtClaimType = HostedAuthClaimTypes.RefreshTokenExpiresAt,
        };
    }

    private static DefaultHttpContext CreateHttpContext(HostedGitHubAuthGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddSingleton(gateway);
        services.AddSingleton(Options.Create(new HostedAdmissionControlOptions()));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };

        return httpContext;
    }

    private static CookieValidatePrincipalContext CreateValidateContext(HttpContext httpContext, ClaimsPrincipal principal)
    {
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme);

        return new CookieValidatePrincipalContext(
            httpContext,
            scheme,
            new CookieAuthenticationOptions(),
            ticket);
    }

    private static RedirectContext<CookieAuthenticationOptions> CreateRedirectContext(HttpContext httpContext, string redirectUri)
    {
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));

        return new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            scheme,
            new CookieAuthenticationOptions(),
            new AuthenticationProperties(),
            redirectUri);
    }

    private static ClaimsPrincipal CreatePrincipal(
        GitHubAuthOptions options,
        string accessToken = "access-token-123",
        DateTimeOffset? accessTokenExpiresAtUtc = null,
        string refreshToken = "",
        DateTimeOffset? refreshTokenExpiresAtUtc = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "markheydon"),
            new Claim(ClaimTypes.Name, "markheydon"),
            new Claim(options.HostedOwnerLoginClaimType, "markheydon"),
            new Claim(options.HostedAccessTokenClaimType, accessToken),
        };

        if (accessTokenExpiresAtUtc is { } expiresAtUtc)
        {
            claims.Add(new Claim(options.HostedTokenExpiresAtClaimType, expiresAtUtc.ToString("O")));
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            claims.Add(new Claim(options.HostedRefreshTokenClaimType, refreshToken));
        }

        if (refreshTokenExpiresAtUtc is { } refreshExpiresAtUtc)
        {
            claims.Add(new Claim(options.HostedRefreshTokenExpiresAtClaimType, refreshExpiresAtUtc.ToString("O")));
        }

        claims.Add(new Claim(HostedAuthClaimTypes.OrganisationLogins, "org-one"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static HostedGitHubAuthGateway CreateGateway(Queue<HttpResponseMessage> responses)
    {
        var messageHandler = new QueueMessageHandler(responses);
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(HostedGitHubAuthGateway.HostedGitHubAuthClientName).Returns(httpClient);

        var authOptions = Options.Create(new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
            HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            HostedAccessTokenClaimType = HostedAuthClaimTypes.AccessToken,
            HostedInstallationIdClaimType = HostedAuthClaimTypes.InstallationId,
            HostedTokenExpiresAtClaimType = HostedAuthClaimTypes.TokenExpiresAt,
            HostedRefreshTokenClaimType = HostedAuthClaimTypes.RefreshToken,
            HostedRefreshTokenExpiresAtClaimType = HostedAuthClaimTypes.RefreshTokenExpiresAt,
        });

        var admissionOptions = Options.Create(new HostedAdmissionControlOptions
        {
            HostedOrganisationLoginsClaimType = HostedAuthClaimTypes.OrganisationLogins,
        });

        return new HostedGitHubAuthGateway(httpClientFactory, authOptions, admissionOptions);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload),
        };
    }

    private sealed class QueueMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException($"No queued response for request '{request.RequestUri}'.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
