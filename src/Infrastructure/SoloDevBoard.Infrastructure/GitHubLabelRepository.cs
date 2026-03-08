using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoloDevBoard.Infrastructure;

/// <summary>
/// GitHub REST API implementation of <see cref="ILabelRepository"/> using <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class GitHubLabelRepository : ILabelRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>Initialises a new instance of the <see cref="GitHubLabelRepository"/> class.</summary>
    /// <param name="httpClientFactory">The factory used to create named <see cref="HttpClient"/> instances.</param>
    /// <param name="currentUserContext">The current user context that provides the authenticated GitHub access token.</param>
    public GitHubLabelRepository(IHttpClientFactory httpClientFactory, ICurrentUserContext currentUserContext)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels?per_page=100";

        return await GetPagedAsync<LabelResponseDto, Label>(
                client,
                endpoint,
            dto => dto.ToDomain(repo),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentNullException.ThrowIfNull(label);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels";

        using var response = await client.PostAsJsonAsync(endpoint, LabelUpsertRequestDto.FromDomain(label), JsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var created = await response.Content.ReadFromJsonAsync<LabelResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw CreateInvalidResponseException("Label response was empty.", endpoint);

        return created.ToDomain(repo);
    }

    /// <inheritdoc/>
    public async Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);
        ArgumentNullException.ThrowIfNull(label);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels/{Uri.EscapeDataString(labelName)}";

        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = JsonContent.Create(UpdateLabelRequestDto.FromDomain(label), options: JsonOptions),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var updated = await response.Content.ReadFromJsonAsync<LabelResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw CreateInvalidResponseException("Label response was empty.", endpoint);

        return updated.ToDomain(repo);
    }

    /// <inheritdoc/>
    public async Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels/{Uri.EscapeDataString(labelName)}";

        using var response = await client.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var accessToken = _currentUserContext.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("GitHub access token returned by the current user context is empty.");
        }

        var client = _httpClientFactory.CreateClient(GitHubService.GitHubApiClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"GitHub API request failed with status code {(int)response.StatusCode} ({response.StatusCode}). Response body: {responseBody}",
            null,
            response.StatusCode);
    }

    private static HttpRequestException CreateInvalidResponseException(string message, string endpoint)
        => new($"GitHub API returned an invalid response for endpoint '{endpoint}'. {message}");

    private static async Task<IReadOnlyList<TDomain>> GetPagedAsync<TDto, TDomain>(
        HttpClient client,
        string initialEndpoint,
        Func<TDto, TDomain?> map,
        CancellationToken cancellationToken)
        where TDomain : class
    {
        var results = new List<TDomain>();
        string? nextUrl = initialEndpoint;

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var response = await client.GetAsync(nextUrl, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

            var dtos = await response.Content.ReadFromJsonAsync<List<TDto>>(JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw CreateInvalidResponseException("The list response body was empty.", nextUrl);

            foreach (var dto in dtos)
            {
                var mapped = map(dto);
                if (mapped is not null)
                {
                    results.Add(mapped);
                }
            }

            nextUrl = GetNextPageUrl(response);
        }

        return results;
    }

    private static string? GetNextPageUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            var segments = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                if (!segment.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var startIndex = segment.IndexOf('<');
                var endIndex = segment.IndexOf('>');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    return segment[(startIndex + 1)..endIndex];
                }
            }
        }

        return null;
    }

    private sealed record LabelResponseDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("color")]
        public string Colour { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        public Label ToDomain(string repoName) => new()
        {
            Name = Name,
            Colour = Colour,
            Description = Description ?? string.Empty,
            RepositoryName = repoName,
        };
    }

    private sealed record LabelUpsertRequestDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("color")]
        public required string Colour { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        public static LabelUpsertRequestDto FromDomain(Label label)
            => new()
            {
                Name = label.Name,
                Colour = label.Colour,
                Description = label.Description,
            };
    }

    private sealed record UpdateLabelRequestDto
    {
        [JsonPropertyName("new_name")]
        public required string NewName { get; init; }

        [JsonPropertyName("color")]
        public required string Colour { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        public static UpdateLabelRequestDto FromDomain(Label label)
            => new()
            {
                NewName = label.Name,
                Colour = label.Colour,
                Description = label.Description,
            };
    }
}
