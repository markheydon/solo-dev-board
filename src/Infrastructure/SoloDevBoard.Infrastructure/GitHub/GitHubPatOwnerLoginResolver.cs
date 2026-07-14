using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Resolves the authenticated GitHub user login for a personal access token.</summary>
public sealed class GitHubPatOwnerLoginResolver(
    IHttpClientFactory httpClientFactory,
    IAppVersionService appVersionService,
    ILogger<GitHubPatOwnerLoginResolver> logger)
{
    public const string HttpClientName = "GitHubPatOwnerLoginResolver";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly IAppVersionService _appVersionService = appVersionService ?? throw new ArgumentNullException(nameof(appVersionService));
    private readonly ILogger<GitHubPatOwnerLoginResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Calls GitHub <c>GET /user</c> and returns the authenticated account login.</summary>
    public async Task<string> ResolveAsync(string personalAccessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personalAccessToken);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
        request.Headers.UserAgent.ParseAdd(_appVersionService.UserAgent);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Failed to resolve GitHub owner login from PAT. Status: {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                body);

            throw new InvalidOperationException(
                "Could not resolve your GitHub login from the configured personal access token. " +
                "Check that GitHubAuth:PersonalAccessToken is valid and has not expired.");
        }

        var user = await response.Content.ReadFromJsonAsync<GitHubAuthenticatedUserResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (user is null || string.IsNullOrWhiteSpace(user.Login))
        {
            throw new InvalidOperationException(
                "GitHub returned an unexpected response when resolving the owner login from the personal access token.");
        }

        return user.Login;
    }

    private sealed class GitHubAuthenticatedUserResponse
    {
        [JsonPropertyName("login")]
        public string Login { get; init; } = string.Empty;
    }
}
