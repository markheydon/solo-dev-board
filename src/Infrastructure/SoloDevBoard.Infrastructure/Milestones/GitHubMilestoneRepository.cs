using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Infrastructure.GitHub;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoloDevBoard.Infrastructure.Milestones;

/// <summary>
/// GitHub REST API implementation of <see cref="IMilestoneRepository"/> using <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class GitHubMilestoneRepository : IMilestoneRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initialises a new instance of the <see cref="GitHubMilestoneRepository"/> class.</summary>
    /// <param name="httpClientFactory">The factory used to create named <see cref="HttpClient"/> instances.</param>
    public GitHubMilestoneRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/milestones?state=all&per_page=100";

        return await GitHubService.GetPagedAsync<MilestoneResponseDto, Milestone>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Milestone> CreateMilestoneAsync(string owner, string repo, Milestone milestone, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentNullException.ThrowIfNull(milestone);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/milestones";

        using var response = await client.PostAsJsonAsync(endpoint, MilestoneUpsertRequestDto.FromDomain(milestone), JsonOptions, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var created = await response.Content.ReadFromJsonAsync<MilestoneResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw GitHubService.CreateInvalidResponseException("Milestone response was empty.", endpoint);

        return created.ToDomain();
    }

    /// <inheritdoc/>
    public async Task<Milestone> UpdateMilestoneAsync(string owner, string repo, int milestoneNumber, Milestone milestone, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        if (milestoneNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milestoneNumber), "Milestone number must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(milestone);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/milestones/{milestoneNumber}";

        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = JsonContent.Create(MilestoneUpsertRequestDto.FromDomain(milestone), options: JsonOptions),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var updated = await response.Content.ReadFromJsonAsync<MilestoneResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw GitHubService.CreateInvalidResponseException("Milestone response was empty.", endpoint);

        return updated.ToDomain();
    }

    /// <inheritdoc/>
    public async Task DeleteMilestoneAsync(string owner, string repo, int milestoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        if (milestoneNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milestoneNumber), "Milestone number must be greater than zero.");
        }

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/milestones/{milestoneNumber}";

        using var response = await client.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private HttpClient CreateClient()
    {
        // Authentication is handled by the configured GitHubAuthHandler on the named HttpClient.
        return _httpClientFactory.CreateClient(GitHubService.GitHubApiClientName);
    }

    private sealed record MilestoneResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("number")]
        public int Number { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("state")]
        public string State { get; init; } = string.Empty;

        [JsonPropertyName("due_on")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? DueOn { get; init; }

        [JsonPropertyName("open_issues")]
        public int OpenIssues { get; init; }

        [JsonPropertyName("closed_issues")]
        public int ClosedIssues { get; init; }

        public Milestone ToDomain() => new()
        {
            Id = GitHubService.ConvertGitHubIdToInt(Id),
            Number = Number,
            Title = Title,
            Description = Description ?? string.Empty,
            State = State,
            DueOn = DueOn,
            OpenIssues = OpenIssues,
            ClosedIssues = ClosedIssues,
        };
    }

    private sealed record MilestoneUpsertRequestDto
    {
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [JsonPropertyName("state")]
        public required string State { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("due_on")]
        public DateTimeOffset? DueOn { get; init; }

        public static MilestoneUpsertRequestDto FromDomain(Milestone milestone)
            => new()
            {
                Title = milestone.Title,
                State = string.IsNullOrWhiteSpace(milestone.State) ? "open" : milestone.State,
                Description = milestone.Description,
                DueOn = milestone.DueOn,
            };
    }
}
