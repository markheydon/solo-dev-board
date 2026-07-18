using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.Workflows;
using SoloDevBoard.Domain.Entities.Workflows;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Workflows;

/// <summary>GitHub REST API implementation of <see cref="IWorkflowFileRepository"/>.</summary>
public sealed class GitHubWorkflowFileRepository : IWorkflowFileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initialises a new instance of the <see cref="GitHubWorkflowFileRepository"/> class.</summary>
    /// <param name="httpClientFactory">The factory used to create named <see cref="HttpClient"/> instances.</param>
    public GitHubWorkflowFileRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc/>
    public async Task<WorkflowFile?> GetWorkflowFileAsync(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{path}";

        using var response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var fileResponse = await response.Content.ReadFromJsonAsync<WorkflowFileResponseDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw GitHubService.CreateInvalidResponseException("Workflow file response was empty.", endpoint);

        return new WorkflowFile
        {
            Path = fileResponse.Path ?? path,
            Content = DecodeContent(fileResponse.Content),
            Sha = fileResponse.Sha,
        };
    }

    /// <inheritdoc/>
    public async Task CreateOrUpdateWorkflowFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string? existingSha,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(commitMessage);

        var client = CreateClient();
        var endpoint = $"/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{path}";
        var request = new WorkflowFileUpsertRequestDto
        {
            Message = commitMessage,
            Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            Sha = existingSha,
        };

        using var response = await client.PutAsJsonAsync(endpoint, request, JsonOptions, cancellationToken).ConfigureAwait(false);
        await GitHubService.EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static string DecodeContent(string? encodedContent)
    {
        if (string.IsNullOrWhiteSpace(encodedContent))
        {
            return string.Empty;
        }

        var normalised = encodedContent.ReplaceLineEndings(string.Empty);
        var bytes = Convert.FromBase64String(normalised);
        return Encoding.UTF8.GetString(bytes);
    }

    private HttpClient CreateClient()
        => _httpClientFactory.CreateClient(GitHubService.GitHubApiClientName);

    private sealed class WorkflowFileResponseDto
    {
        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("sha")]
        public string? Sha { get; init; }

        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private sealed class WorkflowFileUpsertRequestDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("sha")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Sha { get; init; }
    }
}
