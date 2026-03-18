using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>GitHub REST API implementation of <see cref="IGitHubService"/> using <see cref="IHttpClientFactory"/>.</summary>
public sealed class GitHubService : IGitHubService
{
    /// <summary>Name of the configured GitHub API <see cref="HttpClient"/>.</summary>
    public const string GitHubApiClientName = "GitHubApiClient";

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initialises a new instance of the <see cref="GitHubService"/> class.</summary>
    /// <param name="httpClientFactory">The factory used to create named <see cref="HttpClient"/> instances.</param>
    public GitHubService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateAuthenticatedClient();
        const string endpoint = "/user/repos?sort=updated&per_page=100";
        return await GetPagedAsync<RepositoryResponseDto, Repository>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await GetRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        return repositories
            .Where(repository => !repository.IsArchived)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/users/{Uri.EscapeDataString(owner)}/repos?per_page=100";
        return await GetPagedAsync<RepositoryResponseDto, Repository>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(string owner, CancellationToken cancellationToken = default)
    {
        var repositories = await GetRepositoriesAsync(owner, cancellationToken).ConfigureAwait(false);
        return repositories
            .Where(repository => !repository.IsArchived)
            .ToArray();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The GitHub <c>/issues</c> endpoint returns both issues and pull requests. Items with a
    /// non-<see langword="null"/> <c>pull_request</c> marker property are filtered out so that
    /// only genuine issues are returned.
    /// </remarks>
    public async Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues?state=all&per_page=100";
        var issues = await GetPagedAsync<IssueResponseDto, Issue>(
                client,
                endpoint,
                static dto => dto.PullRequest is null ? dto.ToDomain() : null,
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return issues;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/pulls?state=all&per_page=100";
        var pullRequests = await GetPagedAsync<PullRequestResponseDto, PullRequest>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return pullRequests;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/actions/runs?per_page=25";

        using var response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var workflowRunsResponse = await response.Content.ReadFromJsonAsync<WorkflowRunsResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw CreateInvalidResponseException("Workflow runs response was empty.", endpoint);

        return workflowRunsResponse.WorkflowRuns
            .ConvertAll(static workflowRun => workflowRun.ToDomain());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/milestones?state=all&per_page=100";
        var milestones = await GetPagedAsync<MilestoneResponseDto, Milestone>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return milestones;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/labels?per_page=100";
        var labels = await GetPagedAsync<LabelResponseDto, Label>(
                client,
                endpoint,
                static dto => dto.ToDomain(),
            JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return labels;
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

        return created.ToDomain();
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

        return updated.ToDomain();
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

    /// <inheritdoc/>
    public async Task ApplyLabelsToTriageItemAsync(string owner, string repo, int itemNumber, IReadOnlyList<string> labelNames, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        if (itemNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemNumber), "Item number must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(labelNames);

        var normalisedLabelNames = labelNames
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{itemNumber}/labels";

        using var response = await client.PutAsJsonAsync(
                endpoint,
                new TriageLabelsRequestDto(normalisedLabelNames),
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AssignMilestoneToTriageItemAsync(string owner, string repo, int itemNumber, int? milestoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        if (itemNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemNumber), "Item number must be greater than zero.");
        }

        if (milestoneNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milestoneNumber), "Milestone number must be greater than zero when provided.");
        }

        var client = CreateAuthenticatedClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{itemNumber}";

        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = JsonContent.Create(new TriageMilestoneRequestDto(milestoneNumber), options: JsonOptions),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> AddTriageItemToProjectBoardAsync(string owner, string repo, int itemNumber, string projectId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        if (itemNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemNumber), "Item number must be greater than zero.");
        }

        var client = CreateAuthenticatedClient();
        var itemEndpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{itemNumber}";

        using var itemResponse = await client.GetAsync(itemEndpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(itemResponse, cancellationToken).ConfigureAwait(false);

        var itemNode = await itemResponse.Content.ReadFromJsonAsync<TriageItemNodeResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw CreateInvalidResponseException("Triage item response was empty.", itemEndpoint);

        if (string.IsNullOrWhiteSpace(itemNode.NodeId))
        {
            throw CreateInvalidResponseException("Triage item did not include a node identifier.", itemEndpoint);
        }

        const string mutation = "mutation AddProjectV2Item($projectId: ID!, $contentId: ID!) { addProjectV2ItemById(input: { projectId: $projectId, contentId: $contentId }) { item { id } } }";
        var requestBody = new GraphQlRequestDto(
            mutation,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectId"] = projectId,
                ["contentId"] = itemNode.NodeId,
            });

        using var graphQlResponse = await client.PostAsJsonAsync("/graphql", requestBody, JsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(graphQlResponse, cancellationToken).ConfigureAwait(false);

        var graphQlPayload = await graphQlResponse.Content.ReadFromJsonAsync<AddProjectV2ItemResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw CreateInvalidResponseException("GraphQL response body was empty.", "/graphql");

        if (graphQlPayload.Errors.Count > 0)
        {
            var combinedErrors = string.Join("; ", graphQlPayload.Errors.Select(static error => error.Message));
            throw new HttpRequestException($"GitHub GraphQL request failed. Errors: {combinedErrors}");
        }

        var addedItemId = graphQlPayload.Data?.AddProjectV2ItemById?.Item?.Id;
        if (string.IsNullOrWhiteSpace(addedItemId))
        {
            throw CreateInvalidResponseException("GraphQL response did not contain the created project item identifier.", "/graphql");
        }

        return addedItemId;
    }

    /// <inheritdoc/>
    public async Task CloseTriageItemAsDuplicateAsync(string owner, string repo, GitHubTriageItemType itemType, int itemNumber, string duplicateReference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(duplicateReference);

        if (itemNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemNumber), "Item number must be greater than zero.");
        }

        var trimmedReference = duplicateReference.Trim();
        var client = CreateAuthenticatedClient();

        var commentEndpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{itemNumber}/comments";
        var comment = new DuplicateCommentRequestDto($"Duplicate of {trimmedReference}");

        using var commentResponse = await client.PostAsJsonAsync(commentEndpoint, comment, JsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(commentResponse, cancellationToken).ConfigureAwait(false);

        switch (itemType)
        {
            case GitHubTriageItemType.Issue:
            {
                var issueEndpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{itemNumber}";
                using var issueRequest = new HttpRequestMessage(HttpMethod.Patch, issueEndpoint)
                {
                    Content = JsonContent.Create(new IssueStateRequestDto("closed"), options: JsonOptions),
                };

                using var issueResponse = await client.SendAsync(issueRequest, cancellationToken).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(issueResponse, cancellationToken).ConfigureAwait(false);
                break;
            }
            case GitHubTriageItemType.PullRequest:
            {
                var pullRequestEndpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/pulls/{itemNumber}";
                using var pullRequestRequest = new HttpRequestMessage(HttpMethod.Patch, pullRequestEndpoint)
                {
                    Content = JsonContent.Create(new PullRequestStateRequestDto("closed"), options: JsonOptions),
                };

                using var pullRequestResponse = await client.SendAsync(pullRequestRequest, cancellationToken).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(pullRequestResponse, cancellationToken).ConfigureAwait(false);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, "Unsupported triage item type.");
        }
    }

    private HttpClient CreateAuthenticatedClient()
    {
        // Authentication is handled by the configured GitHubAuthHandler on the named HttpClient.
        return _httpClientFactory.CreateClient(GitHubApiClientName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Reads the response body and throws an <see cref="HttpRequestException"/> if the response
    /// does not indicate success, including the status code and body in the exception message.
    /// </summary>
    /// <param name="response">The HTTP response to inspect.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    internal static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

    /// <summary>Creates an <see cref="HttpRequestException"/> describing an unexpected or empty API response body.</summary>
    /// <param name="message">A description of the specific problem with the response.</param>
    /// <param name="endpoint">The API endpoint URL that produced the invalid response.</param>
    internal static HttpRequestException CreateInvalidResponseException(string message, string endpoint)
        => new($"GitHub API returned an invalid response for endpoint '{endpoint}'. {message}");

    /// <summary>
    /// Converts a GitHub numeric identifier to a 32-bit <see cref="int"/> value using an unchecked cast.
    /// This is a lossy mapping intended only for scenarios that do not require the full unique identifier.
    /// Callers must not rely on the returned value being unique across all GitHub entities.
    /// </summary>
    /// <param name="id">The GitHub identifier to convert.</param>
    /// <returns>An <see cref="int"/> value derived from the GitHub identifier, suitable for non-unique mapping.</returns>
    internal static int ConvertGitHubIdToInt(long id)
    {
        unchecked
        {
            return (int)id;
        }
    }

    /// <summary>
    /// Repairs common mojibake artefacts seen in externally sourced text.
    /// This preserves user readability when punctuation has been decoded incorrectly upstream,
    /// normalising malformed dash-like sequences to ASCII separator text.
    /// </summary>
    /// <param name="value">The source text to repair.</param>
    /// <returns>A cleaned string suitable for UI display.</returns>
    internal static string RepairCommonMojibake(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\u00D4\u00C7\u00F6", " - ", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u201D", " - ", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u201C", " - ", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u2122", "'", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u0153", "\"", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u009D", "\"", StringComparison.Ordinal)
            .Replace("\u00E2\u20AC\u00A6", "...", StringComparison.Ordinal)
            .Replace("\u00C2", string.Empty, StringComparison.Ordinal)
            .Replace("  -  ", " - ", StringComparison.Ordinal)
            .Replace("  - ", " - ", StringComparison.Ordinal)
            .Replace(" -  ", " - ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    /// <summary>
    /// Fetches all pages of a paged GitHub API endpoint and accumulates mapped domain entities
    /// across all pages, following <c>Link: rel="next"</c> headers until no further pages exist.
    /// </summary>
    /// <typeparam name="TDto">The DTO type deserialised from each page of the GitHub API response.</typeparam>
    /// <typeparam name="TDomain">The domain entity type produced by the mapping function.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> configured for the GitHub API.</param>
    /// <param name="initialEndpoint">The relative URL of the first page to fetch.</param>
    /// <param name="map">
    /// A function mapping a DTO to a domain entity. Return <see langword="null"/> to exclude an item
    /// from the results (e.g. to filter pull requests out of an issues response).
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of all mapped domain entities across all pages.</returns>
    internal static async Task<IReadOnlyList<TDomain>> GetPagedAsync<TDto, TDomain>(
        HttpClient client,
        string initialEndpoint,
        Func<TDto, TDomain?> map,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
        where TDomain : class
    {
        var results = new List<TDomain>();
        // Start with the caller-supplied URL; GetNextPageUrl returns the next page URL or null when exhausted.
        string? nextUrl = initialEndpoint;

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var response = await client.GetAsync(nextUrl, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

            var dtos = await response.Content.ReadFromJsonAsync<List<TDto>>(jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw CreateInvalidResponseException("The list response body was empty.", nextUrl);

            foreach (var dto in dtos)
            {
                var mapped = map(dto);
                if (mapped is not null)
                {
                    results.Add(mapped);
                }
            }

            // Advance to the next page URL extracted from the Link response header, or null to exit the loop.
            nextUrl = GetNextPageUrl(response);
        }

        return results;
    }

    /// <summary>
    /// Extracts the URL of the next page from the HTTP <c>Link</c> response header, or returns
    /// <see langword="null"/> if no next-page link is present.
    /// </summary>
    /// <remarks>
    /// GitHub paginates results via a <c>Link</c> header in the form:
    /// <c>&lt;https://api.github.com/...?page=2&gt;; rel="next", &lt;...&gt;; rel="last"</c>.
    /// This method parses that header and returns the URL for the <c>rel="next"</c> entry.
    /// </remarks>
    internal static string? GetNextPageUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            // Each Link header value may contain multiple comma-separated entries.
            var segments = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                if (!segment.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Extract the URL from within the angle brackets: <https://api.github.com/...>
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

    /// <summary>DTO for a repository returned by the GitHub <c>GET /users/{owner}/repos</c> endpoint.</summary>
    private sealed record RepositoryResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("html_url")]
        public string Url { get; init; } = string.Empty;

        [JsonPropertyName("private")]
        public bool IsPrivate { get; init; }

        [JsonPropertyName("archived")]
        public bool IsArchived { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }

        public Repository ToDomain() => new()
        {
            Id = ConvertGitHubIdToInt(Id),
            Name = Name,
            FullName = FullName,
            Description = Description ?? string.Empty,
            Url = Url,
            IsPrivate = IsPrivate,
            IsArchived = IsArchived,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }

    /// <summary>
    /// DTO for an item returned by the GitHub <c>GET /repos/{owner}/{repo}/issues</c> endpoint.
    /// The endpoint returns both issues and pull requests; items where <see cref="PullRequest"/> is
    /// non-<see langword="null"/> are pull requests and must be excluded from issue results.
    /// </summary>
    private sealed record IssueResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("number")]
        public int Number { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("state")]
        public string State { get; init; } = string.Empty;

        [JsonPropertyName("user")]
        public UserResponseDto? User { get; init; }

        [JsonPropertyName("labels")]
        public List<LabelResponseDto> Labels { get; init; } = [];

        [JsonPropertyName("milestone")]
        public MilestoneResponseDto? Milestone { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }

        [JsonPropertyName("pull_request")]
        public PullRequestMarkerDto? PullRequest { get; init; }

        public Issue ToDomain() => new()
        {
            Id = ConvertGitHubIdToInt(Id),
            Number = Number,
            Title = Title,
            HtmlUrl = HtmlUrl ?? string.Empty,
            Body = Body ?? string.Empty,
            State = State,
            AuthorLogin = User?.Login ?? string.Empty,
            Labels = Labels.ConvertAll(static label => label.ToDomain()),
            Milestone = Milestone?.ToDomain(),
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }

    /// <summary>DTO for a pull request returned by the GitHub <c>GET /repos/{owner}/{repo}/pulls</c> endpoint.</summary>
    private sealed record PullRequestResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("number")]
        public int Number { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("state")]
        public string State { get; init; } = string.Empty;

        [JsonPropertyName("user")]
        public UserResponseDto? User { get; init; }

        [JsonPropertyName("head")]
        public BranchResponseDto? Head { get; init; }

        [JsonPropertyName("base")]
        public BranchResponseDto? Base { get; init; }

        [JsonPropertyName("draft")]
        public bool IsDraft { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }

        public PullRequest ToDomain() => new()
        {
            Id = ConvertGitHubIdToInt(Id),
            Number = Number,
            Title = Title,
            HtmlUrl = HtmlUrl ?? string.Empty,
            Body = Body ?? string.Empty,
            State = State,
            AuthorLogin = User?.Login ?? string.Empty,
            HeadBranch = Head?.ReferenceName ?? string.Empty,
            BaseBranch = Base?.ReferenceName ?? string.Empty,
            IsDraft = IsDraft,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }

    /// <summary>DTO wrapper for workflow runs returned by the GitHub <c>GET /repos/{owner}/{repo}/actions/runs</c> endpoint.</summary>
    private sealed record WorkflowRunsResponseDto
    {
        [JsonPropertyName("workflow_runs")]
        public List<WorkflowRunResponseDto> WorkflowRuns { get; init; } = [];
    }

    /// <summary>DTO for a workflow run returned by the GitHub <c>GET /repos/{owner}/{repo}/actions/runs</c> endpoint.</summary>
    private sealed record WorkflowRunResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("conclusion")]
        public string? Conclusion { get; init; }

        [JsonPropertyName("head_branch")]
        public string? HeadBranch { get; init; }

        [JsonPropertyName("head_sha")]
        public string? HeadSha { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        public WorkflowRun ToDomain() => new()
        {
            Id = ConvertGitHubIdToInt(Id),
            WorkflowName = Name,
            Status = Status ?? string.Empty,
            Conclusion = Conclusion ?? string.Empty,
            HeadBranch = HeadBranch ?? string.Empty,
            HeadSha = HeadSha ?? string.Empty,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            HtmlUrl = HtmlUrl ?? string.Empty,
        };
    }

    /// <summary>DTO for a milestone returned by the GitHub <c>GET /repos/{owner}/{repo}/milestones</c> endpoint.</summary>
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
        public DateTimeOffset? DueOn { get; init; }

        [JsonPropertyName("open_issues")]
        public int OpenIssues { get; init; }

        [JsonPropertyName("closed_issues")]
        public int ClosedIssues { get; init; }

        public Milestone ToDomain() => new()
        {
            Id = ConvertGitHubIdToInt(Id),
            Number = Number,
            Title = Title,
            Description = Description ?? string.Empty,
            State = State,
            DueOn = DueOn,
            OpenIssues = OpenIssues,
            ClosedIssues = ClosedIssues,
        };
    }

    /// <summary>DTO for a label returned by the GitHub labels API endpoints.</summary>
    private sealed record LabelResponseDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("color")]
        public string Colour { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        public Label ToDomain() => new()
        {
            Name = Name,
            Colour = Colour,
            Description = RepairCommonMojibake(Description),
        };
    }

    /// <summary>Embedded DTO representing the author of an issue or pull request.</summary>
    private sealed record UserResponseDto
    {
        [JsonPropertyName("login")]
        public string Login { get; init; } = string.Empty;
    }

    /// <summary>Embedded DTO representing a branch reference on a pull request (head or base branch).</summary>
    private sealed record BranchResponseDto
    {
        [JsonPropertyName("ref")]
        public string ReferenceName { get; init; } = string.Empty;
    }

    /// <summary>
    /// Marker DTO for the <c>pull_request</c> property present on issue items that are actually pull requests.
    /// A non-<see langword="null"/> value indicates the item should be treated as a pull request, not an issue.
    /// </summary>
    private sealed record PullRequestMarkerDto;

    /// <summary>Request body DTO for creating a new label via <c>POST /repos/{owner}/{repo}/labels</c>.</summary>
    private sealed record LabelUpsertRequestDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("color")]
        public string Colour { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        public static LabelUpsertRequestDto FromDomain(Label label) => new()
        {
            Name = label.Name,
            Colour = label.Colour,
            Description = label.Description,
        };
    }

    /// <summary>
    /// Request body DTO for renaming or updating an existing label via <c>PATCH /repos/{owner}/{repo}/labels/{name}</c>.
    /// Uses <c>new_name</c> rather than <c>name</c> to rename the label, per the GitHub API contract.
    /// </summary>
    private sealed record UpdateLabelRequestDto
    {
        [JsonPropertyName("new_name")]
        public string NewName { get; init; } = string.Empty;

        [JsonPropertyName("color")]
        public string Colour { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        public static UpdateLabelRequestDto FromDomain(Label label) => new()
        {
            NewName = label.Name,
            Colour = label.Colour,
            Description = label.Description,
        };
    }

    /// <summary>Request body DTO for setting labels on a triage item.</summary>
    private sealed record TriageLabelsRequestDto(
        [property: JsonPropertyName("labels")] IReadOnlyList<string> Labels);

    /// <summary>Request body DTO for assigning or clearing a milestone on a triage item.</summary>
    private sealed record TriageMilestoneRequestDto(
        [property: JsonPropertyName("milestone")] int? Milestone);

    /// <summary>Request body DTO for posting duplicate-reference comments.</summary>
    private sealed record DuplicateCommentRequestDto(
        [property: JsonPropertyName("body")] string Body);

    /// <summary>Request body DTO for issue state updates.</summary>
    private sealed record IssueStateRequestDto(
        [property: JsonPropertyName("state")] string State);

    /// <summary>Request body DTO for pull request state updates.</summary>
    private sealed record PullRequestStateRequestDto(
        [property: JsonPropertyName("state")] string State);

    /// <summary>Request body DTO for GitHub GraphQL requests.</summary>
    private sealed record GraphQlRequestDto(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("variables")] IReadOnlyDictionary<string, string> Variables);

    /// <summary>DTO for resolving a triage item GraphQL node identifier from the issues REST endpoint.</summary>
    private sealed record TriageItemNodeResponseDto
    {
        [JsonPropertyName("node_id")]
        public string NodeId { get; init; } = string.Empty;
    }

    /// <summary>DTO wrapper for GraphQL add-project-item responses.</summary>
    private sealed record AddProjectV2ItemResponseDto
    {
        [JsonPropertyName("data")]
        public AddProjectV2ItemDataDto? Data { get; init; }

        [JsonPropertyName("errors")]
        public IReadOnlyList<GraphQlErrorDto> Errors { get; init; } = [];
    }

    /// <summary>DTO for GraphQL response data payload.</summary>
    private sealed record AddProjectV2ItemDataDto
    {
        [JsonPropertyName("addProjectV2ItemById")]
        public AddProjectV2ItemPayloadDto? AddProjectV2ItemById { get; init; }
    }

    /// <summary>DTO for GraphQL mutation payload.</summary>
    private sealed record AddProjectV2ItemPayloadDto
    {
        [JsonPropertyName("item")]
        public AddProjectV2ItemPayloadItemDto? Item { get; init; }
    }

    /// <summary>DTO for GraphQL mutation item payload.</summary>
    private sealed record AddProjectV2ItemPayloadItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;
    }

    /// <summary>DTO for GraphQL error payloads.</summary>
    private sealed record GraphQlErrorDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
