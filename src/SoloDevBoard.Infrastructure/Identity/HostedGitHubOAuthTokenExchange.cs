using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Infrastructure.Identity;

/// <inheritdoc/>
public sealed class HostedGitHubOAuthTokenExchange(
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubAuthOptions> authOptions) : IHostedGitHubOAuthTokenExchange
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc/>
    public async Task<HostedGitHubRefreshTokenExchangeResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var options = authOptions.Value;
        var client = httpClientFactory.CreateClient("HostedGitHubOAuthClient");

        using var request = new HttpRequestMessage(HttpMethod.Post, options.HostedGitHubAccessTokenEndpoint);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = options.HostedGitHubAppClientId,
            ["client_secret"] = options.HostedGitHubAppClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Hosted token refresh failed because the GitHub access-token response was empty.");

        if (!string.IsNullOrWhiteSpace(payload.Error) || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Hosted token refresh failed because GitHub did not return a valid access token.");
        }

        var refresh = string.IsNullOrWhiteSpace(payload.RefreshToken) ? refreshToken : payload.RefreshToken;
        return new HostedGitHubRefreshTokenExchangeResult(
            payload.AccessToken,
            refresh,
            ComputeExpiresAtUtc(payload.RefreshTokenExpiresInSeconds),
            ComputeExpiresAtUtc(payload.ExpiresInSeconds));
    }

    private static DateTimeOffset? ComputeExpiresAtUtc(int? expiresInSeconds)
    {
        if (expiresInSeconds is null or <= 0)
        {
            return null;
        }

        return DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value);
    }

    private sealed class AccessTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresInSeconds { get; set; }

        [JsonPropertyName("refresh_token_expires_in")]
        public int? RefreshTokenExpiresInSeconds { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
