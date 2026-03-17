using Moq;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Domain.Entities.BoardRules;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Labels;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubLabelRepository"/>.</summary>
public sealed class GitHubLabelRepositoryTests
{
    [Fact]
    public async Task GetLabelsAsync_ValidResponse_ReturnsMappedLabels()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "enhancement",
                    "color": "a2eeef",
                    "description": null
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetLabelsAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal("enhancement", result[0].Name);
        Assert.Equal("a2eeef", result[0].Colour);
        Assert.Equal(string.Empty, result[0].Description);
        Assert.Equal("repo", result[0].RepositoryName);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels?per_page=100", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization?.Scheme);
        Assert.Equal("test-token", handler.Requests[0].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task CreateLabelAsync_ValidLabel_PostsPayloadAndReturnsLabel()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "name": "bug",
                  "color": "d73a4a",
                  "description": "Something is not working"
                }
                """),
        ]);

        var sut = CreateSubject(handler);
        var label = new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working" };

        // Act
        var result = await sut.CreateLabelAsync("owner", "repo", label);

        // Assert
        Assert.Equal("bug", result.Name);
        Assert.Equal("d73a4a", result.Colour);
        Assert.Equal("Something is not working", result.Description);
        Assert.Equal("repo", result.RepositoryName);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("bug", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("d73a4a", document.RootElement.GetProperty("color").GetString());
        Assert.Equal("Something is not working", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdateLabelAsync_ValidLabel_SendsPatchPayloadWithNewName()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "name": "enhancement",
                  "color": "a2eeef",
                  "description": "Feature request"
                }
                """),
        ]);

        var sut = CreateSubject(handler);
        var label = new Label { Name = "enhancement", Colour = "a2eeef", Description = "Feature request" };

        // Act
        var result = await sut.UpdateLabelAsync("owner", "repo", "feature", label);

        // Assert
        Assert.Equal("enhancement", result.Name);
        Assert.Equal("Feature request", result.Description);
        Assert.Equal("repo", result.RepositoryName);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels/feature", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("enhancement", document.RootElement.GetProperty("new_name").GetString());
        Assert.Equal("a2eeef", document.RootElement.GetProperty("color").GetString());
        Assert.Equal("Feature request", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task DeleteLabelAsync_ValidLabelName_SendsDeleteRequest()
    {
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.NoContent),
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.DeleteLabelAsync("owner", "repo", "bug");

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels/bug", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task CreateLabelAsync_ApiReturnsBadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"Validation failed\"}", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act / Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => _ = await sut.CreateLabelAsync("owner", "repo", new Label { Name = "bug", Colour = "d73a4a" }));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("GitHub API request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLabelsAsync_CurrentUserTokenMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock
            .Setup(context => context.GetAccessToken())
            .Throws(new InvalidOperationException("Token missing."));

        var authHandler = new GitHubAuthHandler(currentUserContextMock.Object)
        {
            InnerHandler = new QueueMessageHandler([new HttpResponseMessage(HttpStatusCode.OK)]),
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(GitHubService.GitHubApiClientName))
            .Returns(client);

        var sut = new GitHubLabelRepository(httpClientFactoryMock.Object);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => _ = await sut.GetLabelsAsync("owner", "repo"));
    }

    private static GitHubLabelRepository CreateSubject(HttpMessageHandler handler)
    {
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock
            .Setup(context => context.GetAccessToken())
            .Returns("test-token");

        var authHandler = new GitHubAuthHandler(currentUserContextMock.Object)
        {
            InnerHandler = handler,
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(GitHubService.GitHubApiClientName))
            .Returns(client);

        return new GitHubLabelRepository(httpClientFactoryMock.Object);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class QueueMessageHandler(IEnumerable<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await CloneRequestAsync(request, cancellationToken).ConfigureAwait(false));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No mocked responses are left in the queue.");
            }

            return _responses.Dequeue();
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var mediaType = request.Content.Headers.ContentType?.MediaType ?? "application/json";
                clone.Content = new StringContent(content, Encoding.UTF8, mediaType);
            }

            return clone;
        }
    }
}
