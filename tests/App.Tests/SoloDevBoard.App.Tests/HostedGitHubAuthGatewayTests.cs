using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.App.Tests;

public sealed class HostedGitHubAuthGatewayTests
{
    [Fact]
    public async Task ExchangeCodeForSessionAsync_ValidResponses_ReturnsSessionWithExpectedClaims()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "token-123", refresh_token = "refresh-123", expires_in = 3600, refresh_token_expires_in = 604800 }),
            CreateJsonResponse(new { login = "markheydon" }),
            CreateJsonResponse(new
            {
                installations = new[]
                {
                    new
                    {
                        id = 987654321,
                        account = new { login = "markheydon" },
                    },
                },
            }),
            CreateJsonResponse(new[]
            {
                new { login = "org-one" },
                new { login = "org-two" },
            }),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var session = await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        Assert.Equal("markheydon", session.OwnerLogin);
        Assert.Equal("token-123", session.AccessToken);
        Assert.Equal(987654321, session.InstallationId);
        Assert.NotNull(session.TokenExpiresAtUtc);
        Assert.Equal("refresh-123", session.RefreshToken);
        Assert.NotNull(session.RefreshTokenExpiresAtUtc);
        Assert.Contains("org-one", session.OrganisationLogins);
        Assert.Contains("org-two", session.OrganisationLogins);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_NoInstallations_ReturnsSessionWithoutInstallationId()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "token-123" }),
            CreateJsonResponse(new { login = "markheydon" }),
            CreateJsonResponse(new { installations = Array.Empty<object>() }),
            CreateJsonResponse(Array.Empty<object>()),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var session = await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        Assert.Equal("markheydon", session.OwnerLogin);
        Assert.Equal("token-123", session.AccessToken);
        Assert.Null(session.InstallationId);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_TokenExchangeFails_ThrowsHttpRequestException()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request"),
            },
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var act = async () => await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_TokenPayloadContainsError_ThrowsInvalidOperationException()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { error = "bad_verification_code" }),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var act = async () => await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_UserResponseMissingLogin_ThrowsInvalidOperationException()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "token-123" }),
            CreateJsonResponse(new { login = "" }),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var act = async () => await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_NoOwnerInstallationMatch_FallsBackToFirstInstallation()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "token-123" }),
            CreateJsonResponse(new { login = "markheydon" }),
            CreateJsonResponse(new
            {
                installations = new[]
                {
                    new
                    {
                        id = 111111111,
                        account = new { login = "other-user" },
                    },
                    new
                    {
                        id = 222222222,
                        account = new { login = "another-user" },
                    },
                },
            }),
            CreateJsonResponse(Array.Empty<object>()),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var session = await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        Assert.Equal(111111111, session.InstallationId);
    }

    [Fact]
    public async Task ExchangeCodeForSessionAsync_OrganisationResponseContainsDuplicates_ReturnsDistinctOrganisationLogins()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "token-123" }),
            CreateJsonResponse(new { login = "markheydon" }),
            CreateJsonResponse(new { installations = Array.Empty<object>() }),
            CreateJsonResponse(new[]
            {
                new { login = "org-one" },
                new { login = "org-one" },
                new { login = "" },
                new { login = "org-two" },
            }),
        ]);
        var gateway = CreateSubject(responses);

        // Act
        var session = await gateway.ExchangeCodeForSessionAsync("code-123", CancellationToken.None);

        // Assert
        Assert.Equal(2, session.OrganisationLogins.Count);
        Assert.Contains("org-one", session.OrganisationLogins);
        Assert.Contains("org-two", session.OrganisationLogins);
    }

    [Fact]
    public void CreatePrincipal_ValidSession_MapsRequiredHostedClaims()
    {
        // Arrange
        var gateway = CreateSubject(new Queue<HttpResponseMessage>());
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            987654321,
            DateTimeOffset.UtcNow.AddHours(1),
            ["org-one", "org-two"],
            "refresh-token-123",
            DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var principal = gateway.CreatePrincipal(session);

        // Assert
        Assert.Equal("markheydon", principal.FindFirstValue(HostedAuthClaimTypes.OwnerLogin));
        Assert.Equal("token-123", principal.FindFirstValue(HostedAuthClaimTypes.AccessToken));
        Assert.Equal("987654321", principal.FindFirstValue(HostedAuthClaimTypes.InstallationId));
        Assert.NotNull(principal.FindFirstValue(HostedAuthClaimTypes.TokenExpiresAt));
        Assert.Equal("refresh-token-123", principal.FindFirstValue(HostedAuthClaimTypes.RefreshToken));
        Assert.NotNull(principal.FindFirstValue(HostedAuthClaimTypes.RefreshTokenExpiresAt));

        var organisationClaims = principal.FindAll(HostedAuthClaimTypes.OrganisationLogins).Select(claim => claim.Value).ToArray();
        Assert.Equal(2, organisationClaims.Length);
        Assert.Contains("org-one", organisationClaims);
        Assert.Contains("org-two", organisationClaims);
    }

    [Fact]
    public void CreatePrincipal_InstallationMissing_OmitsInstallationClaim()
    {
        // Arrange
        var gateway = CreateSubject(new Queue<HttpResponseMessage>());
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            null,
            DateTimeOffset.UtcNow.AddHours(1),
            [],
            "refresh-token-123",
            DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var principal = gateway.CreatePrincipal(session);

        // Assert
        Assert.Equal("markheydon", principal.FindFirstValue(HostedAuthClaimTypes.OwnerLogin));
        Assert.Equal("token-123", principal.FindFirstValue(HostedAuthClaimTypes.AccessToken));
        Assert.Null(principal.FindFirstValue(HostedAuthClaimTypes.InstallationId));
        Assert.Equal("refresh-token-123", principal.FindFirstValue(HostedAuthClaimTypes.RefreshToken));
    }

    [Fact]
    public async Task RefreshSessionAsync_ValidResponse_ReturnsRefreshedSession()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "new-token-123", refresh_token = "new-refresh-123", expires_in = 3600, refresh_token_expires_in = 604800 }),
        ]);
        var gateway = CreateSubject(responses);
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            987654321,
            DateTimeOffset.UtcNow.AddHours(1),
            ["org-one"],
            "refresh-123",
            DateTimeOffset.UtcNow.AddDays(6));

        // Act
        var refreshed = await gateway.RefreshSessionAsync(session, CancellationToken.None);

        // Assert
        Assert.Equal("markheydon", refreshed.OwnerLogin);
        Assert.Equal("new-token-123", refreshed.AccessToken);
        Assert.Equal("new-refresh-123", refreshed.RefreshToken);
        Assert.Equal(987654321, refreshed.InstallationId);
        Assert.NotNull(refreshed.TokenExpiresAtUtc);
        Assert.NotNull(refreshed.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task RefreshSessionAsync_ResponseOmitsRefreshToken_ReusesExistingRefreshToken()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { access_token = "new-token-123", expires_in = 3600 }),
        ]);
        var gateway = CreateSubject(responses);
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            987654321,
            DateTimeOffset.UtcNow.AddHours(1),
            [],
            "refresh-123",
            null);

        // Act
        var refreshed = await gateway.RefreshSessionAsync(session, CancellationToken.None);

        // Assert
        Assert.Equal("new-token-123", refreshed.AccessToken);
        Assert.Equal("refresh-123", refreshed.RefreshToken);
    }

    [Fact]
    public async Task RefreshSessionAsync_TokenPayloadContainsError_ThrowsInvalidOperationException()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateJsonResponse(new { error = "bad_refresh_token" }),
        ]);
        var gateway = CreateSubject(responses);
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            987654321,
            DateTimeOffset.UtcNow.AddHours(1),
            [],
            "refresh-123",
            null);

        // Act
        var act = async () => await gateway.RefreshSessionAsync(session, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task RefreshSessionAsync_HttpRequestFails_ThrowsHttpRequestException()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") },
        ]);
        var gateway = CreateSubject(responses);
        var session = new HostedGitHubAuthSession(
            "markheydon",
            "token-123",
            987654321,
            DateTimeOffset.UtcNow.AddHours(1),
            [],
            "refresh-123",
            null);

        // Act
        var act = async () => await gateway.RefreshSessionAsync(session, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    private static HostedGitHubAuthGateway CreateSubject(Queue<HttpResponseMessage> responses)
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
