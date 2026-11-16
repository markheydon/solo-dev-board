using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.GitHub;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Mints GitHub App installation tokens for unattended inventory.</summary>
public sealed class GitHubInstallationTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubAuthOptions> authOptions) : IGitHubInstallationTokenProvider
{
    private const string ClientName = "GitHubAppJwtClient";
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt;

    public async Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && _cachedTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }

            var jwt = CreateAppJwt();
            var client = _httpClientFactory.CreateClient(ClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/app/installations/{installationId}/access_tokens");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Headers.Add("Accept", "application/vnd.github+json");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<InstallationTokenResponse>(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Installation token response was empty.");
            _cachedToken = payload.Token;
            _cachedTokenExpiresAt = payload.ExpiresAt;
            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string CreateAppJwt()
    {
        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppId)
            || !AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppPrivateKey))
        {
            throw new InvalidOperationException("GitHub App id and private key must be configured.");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(_authOptions.GitHubAppPrivateKey);
        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        var now = DateTimeOffset.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _authOptions.GitHubAppId,
            IssuedAt = now.UtcDateTime,
            Expires = now.AddMinutes(9).UtcDateTime,
            SigningCredentials = credentials,
        };
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    private sealed class InstallationTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; init; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_at")]
        public DateTimeOffset ExpiresAt { get; init; }
    }
}