using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.App.Authentication;

internal sealed class HostedGitHubAuthGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubAuthOptions> authOptions,
    IOptions<HostedAdmissionControlOptions> admissionOptions)
{
    internal const string HostedGitHubOAuthClientName = "HostedGitHubOAuthClient";

    internal const string HostedGitHubApiClientName = "HostedGitHubApiClient";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly HostedAdmissionControlOptions _admissionOptions = admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));

    internal string BuildAuthoriseUrl(string state, string redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        EnsureHostedSignInConfiguration();

        return QueryHelpers.AddQueryString(
            _authOptions.HostedGitHubAuthoriseEndpoint,
            new Dictionary<string, string?>
            {
                ["client_id"] = _authOptions.HostedGitHubAppClientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = _authOptions.HostedSignInScopes,
                ["state"] = state,
            });
    }

    internal async Task<HostedGitHubAuthSession> ExchangeCodeForSessionAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        EnsureHostedSignInConfiguration();

        var oauthClient = _httpClientFactory.CreateClient(HostedGitHubOAuthClientName);
        var apiClient = _httpClientFactory.CreateClient(HostedGitHubApiClientName);

        var tokenResponse = await ExchangeCodeForAccessTokenAsync(oauthClient, code, cancellationToken).ConfigureAwait(false);
        var user = await GetAuthenticatedUserAsync(apiClient, tokenResponse.AccessToken, cancellationToken).ConfigureAwait(false);
        var installationId = await ResolveInstallationIdAsync(apiClient, tokenResponse.AccessToken, user.Login, cancellationToken).ConfigureAwait(false);
        var organisationLogins = await GetOrganisationLoginsAsync(apiClient, tokenResponse.AccessToken, cancellationToken).ConfigureAwait(false);

        var tokenExpiresAtUtc = ComputeExpiresAtUtc(tokenResponse.ExpiresInSeconds);
        var refreshTokenExpiresAtUtc = ComputeExpiresAtUtc(tokenResponse.RefreshTokenExpiresInSeconds);
        var refreshToken = tokenResponse.RefreshToken ?? string.Empty;

        return new HostedGitHubAuthSession(
            user.Login,
            tokenResponse.AccessToken,
            installationId,
            tokenExpiresAtUtc,
            organisationLogins,
            refreshToken,
            refreshTokenExpiresAtUtc);
    }

    internal async Task<HostedGitHubAuthSession> RefreshSessionAsync(HostedGitHubAuthSession currentSession, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentSession);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentSession.RefreshToken);

        EnsureHostedSignInConfiguration();

        var oauthClient = _httpClientFactory.CreateClient(HostedGitHubOAuthClientName);

        var tokenResponse = await ExchangeRefreshTokenForAccessTokenAsync(oauthClient, currentSession.RefreshToken, cancellationToken).ConfigureAwait(false);

        var tokenExpiresAtUtc = ComputeExpiresAtUtc(tokenResponse.ExpiresInSeconds);
        var refreshTokenExpiresAtUtc = ComputeExpiresAtUtc(tokenResponse.RefreshTokenExpiresInSeconds);
        var refreshToken = !string.IsNullOrWhiteSpace(tokenResponse.RefreshToken)
            ? tokenResponse.RefreshToken
            : currentSession.RefreshToken;

        return new HostedGitHubAuthSession(
            currentSession.OwnerLogin,
            tokenResponse.AccessToken,
            currentSession.InstallationId,
            tokenExpiresAtUtc,
            currentSession.OrganisationLogins,
            refreshToken,
            refreshTokenExpiresAtUtc);
    }

    internal ClaimsPrincipal CreatePrincipal(HostedGitHubAuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.OwnerLogin),
            new(ClaimTypes.Name, session.OwnerLogin),
            new(_authOptions.HostedOwnerLoginClaimType, session.OwnerLogin),
            new(_authOptions.HostedAccessTokenClaimType, session.AccessToken),
        };

        if (!string.IsNullOrWhiteSpace(_authOptions.HostedInstallationIdClaimType) && session.InstallationId is { } installationId)
        {
            claims.Add(new Claim(_authOptions.HostedInstallationIdClaimType, installationId.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(_authOptions.HostedTokenExpiresAtClaimType) && session.TokenExpiresAtUtc is { } expiresAtUtc)
        {
            claims.Add(new Claim(_authOptions.HostedTokenExpiresAtClaimType, expiresAtUtc.ToString("O")));
        }

        if (!string.IsNullOrWhiteSpace(_authOptions.HostedRefreshTokenClaimType) && !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            claims.Add(new Claim(_authOptions.HostedRefreshTokenClaimType, session.RefreshToken));
        }

        if (!string.IsNullOrWhiteSpace(_authOptions.HostedRefreshTokenExpiresAtClaimType) && session.RefreshTokenExpiresAtUtc is { } refreshTokenExpiresAtUtc)
        {
            claims.Add(new Claim(_authOptions.HostedRefreshTokenExpiresAtClaimType, refreshTokenExpiresAtUtc.ToString("O")));
        }

        if (!string.IsNullOrWhiteSpace(_admissionOptions.HostedOrganisationLoginsClaimType))
        {
            foreach (var organisationLogin in session.OrganisationLogins)
            {
                claims.Add(new Claim(_admissionOptions.HostedOrganisationLoginsClaimType, organisationLogin));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private async Task<AccessTokenResponseDto> ExchangeCodeForAccessTokenAsync(HttpClient client, string code, CancellationToken cancellationToken)
    {
        using var request = CreateTokenEndpointRequest(new Dictionary<string, string>
        {
            ["code"] = code,
        });

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccessStatusCode(response);

        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Hosted sign-in failed because the GitHub access-token response was empty.");

        if (!string.IsNullOrWhiteSpace(payload.Error) || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Hosted sign-in failed because GitHub did not return a valid access token.");
        }

        return payload;
    }

    private async Task<AccessTokenResponseDto> ExchangeRefreshTokenForAccessTokenAsync(HttpClient client, string refreshToken, CancellationToken cancellationToken)
    {
        using var request = CreateTokenEndpointRequest(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccessStatusCode(response);

        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Hosted token refresh failed because the GitHub access-token response was empty.");

        if (!string.IsNullOrWhiteSpace(payload.Error) || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Hosted token refresh failed because GitHub did not return a valid access token.");
        }

        return payload;
    }

    private HttpRequestMessage CreateTokenEndpointRequest(Dictionary<string, string> extraFields)
    {
        var formFields = new Dictionary<string, string>
        {
            ["client_id"] = _authOptions.HostedGitHubAppClientId,
            ["client_secret"] = _authOptions.HostedGitHubAppClientSecret,
        };

        foreach (var field in extraFields)
        {
            formFields[field.Key] = field.Value;
        }

        return new HttpRequestMessage(HttpMethod.Post, _authOptions.HostedGitHubAccessTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formFields),
        };
    }

    private static async Task<AuthenticatedUserDto> GetAuthenticatedUserAsync(HttpClient client, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateGitHubApiRequest(HttpMethod.Get, "/user", accessToken);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccessStatusCode(response);

        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Hosted sign-in failed because the GitHub user response was empty.");

        if (string.IsNullOrWhiteSpace(user.Login))
        {
            throw new InvalidOperationException("Hosted sign-in failed because the GitHub user response did not include a login.");
        }

        return user;
    }

    private static async Task<long?> ResolveInstallationIdAsync(HttpClient client, string accessToken, string ownerLogin, CancellationToken cancellationToken)
    {
        using var request = CreateGitHubApiRequest(HttpMethod.Get, "/user/installations", accessToken);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccessStatusCode(response);

        var installations = await response.Content.ReadFromJsonAsync<UserInstallationsResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Hosted sign-in failed because the GitHub installations response was empty.");

        if (installations.Installations.Count == 0)
        {
            return null;
        }

        var matchingInstallation = installations.Installations
            .FirstOrDefault(installation => string.Equals(installation.Account?.Login, ownerLogin, StringComparison.OrdinalIgnoreCase));

        return matchingInstallation?.Id ?? installations.Installations[0].Id;
    }

    private static async Task<IReadOnlyList<string>> GetOrganisationLoginsAsync(HttpClient client, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateGitHubApiRequest(HttpMethod.Get, "/user/orgs", accessToken);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccessStatusCode(response);

        var organisations = await response.Content.ReadFromJsonAsync<List<UserOrganisationDto>>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? [];

        return organisations
            .Select(static organisation => organisation.Login)
            .Where(static login => !string.IsNullOrWhiteSpace(login))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HttpRequestMessage CreateGitHubApiRequest(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return request;
    }

    private void EnsureHostedSignInConfiguration()
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.HostedGitHubAppClientId)
            || !AuthConfigurationPlaceholders.IsConfigured(_authOptions.HostedGitHubAppClientSecret))
        {
            throw new InvalidOperationException(
                "Hosted sign-in is enabled but GitHub App client credentials are missing. Configure HostedGitHubAppClientId and HostedGitHubAppClientSecret.");
        }
    }

    private static DateTimeOffset? ComputeExpiresAtUtc(long? expiresInSeconds)
    {
        if (expiresInSeconds is > 0)
        {
            return DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value);
        }

        return null;
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"GitHub hosted sign-in request failed with status code {(int)response.StatusCode} ({response.StatusCode}).",
            null,
            response.StatusCode);
    }

    private sealed class AccessTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public long? ExpiresInSeconds { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("refresh_token_expires_in")]
        public long? RefreshTokenExpiresInSeconds { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class AuthenticatedUserDto
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }

    private sealed class UserInstallationsResponseDto
    {
        [JsonPropertyName("installations")]
        public List<InstallationDto> Installations { get; set; } = [];
    }

    private sealed class InstallationDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("account")]
        public InstallationAccountDto? Account { get; set; }
    }

    private sealed class InstallationAccountDto
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }

    private sealed class UserOrganisationDto
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }
}

internal sealed record HostedGitHubAuthSession(
    string OwnerLogin,
    string AccessToken,
    long? InstallationId,
    DateTimeOffset? TokenExpiresAtUtc,
    IReadOnlyList<string> OrganisationLogins,
    string RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc);
