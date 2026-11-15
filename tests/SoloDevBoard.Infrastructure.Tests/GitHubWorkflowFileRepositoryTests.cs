using System.Net;
using System.Text;
using NSubstitute;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Workflows;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubWorkflowFileRepository"/>.</summary>
public sealed class GitHubWorkflowFileRepositoryTests
{
    [Fact]
    public async Task ListWorkflowFilesAsync_ReturnsTopLevelYamlFiles()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "ci.yml",
                    "path": ".github/workflows/ci.yml",
                    "type": "file"
                  },
                  {
                    "name": "nested",
                    "path": ".github/workflows/nested",
                    "type": "dir"
                  },
                  {
                    "name": "deploy.yaml",
                    "path": ".github/workflows/deploy.yaml",
                    "type": "file"
                  }
                ]
                """),
        ]);

        var sut = CreateRepository(handler);

        // Act
        var result = await sut.ListWorkflowFilesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, entry => entry.Path == ".github/workflows/ci.yml");
        Assert.Contains(result, entry => entry.Path == ".github/workflows/deploy.yaml");
    }

    [Fact]
    public async Task ListWorkflowFilesAsync_MissingDirectory_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            CreateJsonResponse(HttpStatusCode.NotFound, """{ "message": "Not Found" }"""),
        ]);

        var sut = CreateRepository(handler);

        // Act
        var action = () => sut.ListWorkflowFilesAsync("owner", "missing-repo", cancellationToken);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(action);
    }

    [Fact]
    public async Task ListWorkflowFilesAsync_CalledTwice_UsesCacheOnSecondCall()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "ci.yml",
                    "path": ".github/workflows/ci.yml",
                    "type": "file"
                  }
                ]
                """),
        ]);

        var sut = CreateRepository(handler);

        // Act
        var first = await sut.ListWorkflowFilesAsync("owner", "repo", cancellationToken);
        var second = await sut.ListWorkflowFilesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(first);
        Assert.Single(second);
        Assert.Single(handler.Requests);
    }

    private static GitHubWorkflowFileRepository CreateRepository(QueueMessageHandler handler)
    {
        var currentUserContext = Substitute.For<ICurrentUserContext>();
        currentUserContext.OwnerLogin.Returns("test-user");

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(GitHubService.GitHubApiClientName).Returns(client);

        var responseCache = GitHubCachingTestSupport.CreateResponseCache(currentUserContext: currentUserContext);
        return new GitHubWorkflowFileRepository(httpClientFactory, responseCache);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class QueueMessageHandler(IEnumerable<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No mocked responses are left in the queue.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
