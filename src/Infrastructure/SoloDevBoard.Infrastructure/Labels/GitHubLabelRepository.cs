using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Labels;

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
    private readonly GitHubResponseCache _responseCache;

    /// <summary>Initialises a new instance of the <see cref="GitHubLabelRepository"/> class.</summary>
    /// <param name="httpClientFactory">The factory used to create named <see cref="HttpClient"/> instances.</param>
    /// <param name="responseCache">The cache used for read-heavy GitHub API catalogue responses.</param>
    public GitHubLabelRepository(IHttpClientFactory httpClientFactory, GitHubResponseCache responseCache)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _responseCache = responseCache ?? throw new ArgumentNullException(nameof(responseCache));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        return await _responseCache.GetOrCreateLabelsAsync(
            owner,
            repo,
            async ct =>
            {
                var client = CreateClient();
                var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels?per_page=100";

                return await GitHubService.GetPagedAsync<LabelResponseDto, Label>(
                        client,
                        endpoint,
                        dto => dto.ToDomain(repo),
                        JsonOptions,
                        ct)
                    .ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentNullException.ThrowIfNull(label);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels";

        using var response = await client.PostAsJsonAsync(endpoint, LabelUpsertRequestDto.FromDomain(label), JsonOptions, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var created = await response.Content.ReadFromJsonAsync<LabelResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw GitHubService.CreateInvalidResponseException("Label response was empty.", endpoint);

        _responseCache.InvalidateLabels(owner, repo);

        return created.ToDomain(repo);
    }

    /// <inheritdoc/>
    public async Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);
        ArgumentNullException.ThrowIfNull(label);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels/{Uri.EscapeDataString(labelName)}";

        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = JsonContent.Create(UpdateLabelRequestDto.FromDomain(label), options: JsonOptions),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var updated = await response.Content.ReadFromJsonAsync<LabelResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw GitHubService.CreateInvalidResponseException("Label response was empty.", endpoint);

        _responseCache.InvalidateLabels(owner, repo);

        return updated.ToDomain(repo);
    }

    /// <inheritdoc/>
    public async Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels/{Uri.EscapeDataString(labelName)}";

        using var response = await client.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        _responseCache.InvalidateLabels(owner, repo);
    }

    private HttpClient CreateClient()
    {
        // Authentication is handled by the configured GitHubAuthHandler on the named HttpClient.
        var client = _httpClientFactory.CreateClient(GitHubService.GitHubApiClientName);
        return client;
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
