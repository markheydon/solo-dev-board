using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>GitHub API helper for inventory scans.</summary>
public sealed class InventoryGitHubClient(
    IHttpClientFactory httpClientFactory,
    IInventoryOperatorCredentials credentials)
{
    public const string HttpClientName = "InventoryGitHubApiClient";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly IInventoryOperatorCredentials _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

    public async Task<(bool Found, string? Content, bool NotAccessible)> TryGetFileContentAsync(
        string repositoryFullName,
        string path,
        CancellationToken cancellationToken)
    {
        var token = await _credentials.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var segments = repositoryFullName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            return (false, null, false);
        }

        var encodedPath = Uri.EscapeDataString(path);
        using var response = await client.GetAsync($"/repos/{segments[0]}/{segments[1]}/contents/{encodedPath}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (false, null, false);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return (false, null, true);
        }

        response.EnsureSuccessStatusCode();
        var file = await response.Content.ReadFromJsonAsync<ContentResponse>(cancellationToken).ConfigureAwait(false);
        if (file?.Content is null)
        {
            return (false, null, false);
        }

        var bytes = Convert.FromBase64String(file.Content.Replace("\n", string.Empty, StringComparison.Ordinal));
        return (true, System.Text.Encoding.UTF8.GetString(bytes), false);
    }

    private sealed class ContentResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
