using Moq;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Domain.Entities;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubMilestoneRepository"/>.</summary>
public sealed class GitHubMilestoneRepositoryTests
{
    [Fact]
    public async Task GetMilestonesAsync_ValidResponse_ReturnsMappedMilestones()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 123,
                    "number": 7,
                    "title": "Sprint 7",
                    "description": "Milestone description",
                    "state": "open",
                    "due_on": "2026-04-01T00:00:00Z",
                    "open_issues": 2,
                    "closed_issues": 5
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetMilestonesAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal(123, result[0].Id);
        Assert.Equal(7, result[0].Number);
        Assert.Equal("Sprint 7", result[0].Title);
        Assert.Equal("Milestone description", result[0].Description);
        Assert.Equal("open", result[0].State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-01T00:00:00Z"), result[0].DueOn);
        Assert.Equal(2, result[0].OpenIssues);
        Assert.Equal(5, result[0].ClosedIssues);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/milestones?state=all&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task CreateMilestoneAsync_ValidMilestone_PostsPayloadAndReturnsMilestone()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "id": 321,
                  "number": 8,
                  "title": "Sprint 8",
                  "description": "Next sprint",
                  "state": "open",
                  "due_on": "2026-04-15T00:00:00Z",
                  "open_issues": 0,
                  "closed_issues": 0
                }
                """),
        ]);

        var sut = CreateSubject(handler);
        var milestone = new Milestone
        {
            Id = 0,
            Number = 0,
            Title = "Sprint 8",
            Description = "Next sprint",
            State = "open",
            DueOn = DateTimeOffset.Parse("2026-04-15T00:00:00Z"),
        };

        // Act
        var result = await sut.CreateMilestoneAsync("owner", "repo", milestone);

        // Assert
        Assert.Equal(321, result.Id);
        Assert.Equal(8, result.Number);
        Assert.Equal("Sprint 8", result.Title);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/milestones", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("Sprint 8", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("open", document.RootElement.GetProperty("state").GetString());
        Assert.Equal("Next sprint", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdateMilestoneAsync_ValidMilestone_SendsPatchPayload()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "id": 321,
                  "number": 8,
                  "title": "Sprint 8",
                  "description": "Updated description",
                  "state": "open",
                  "due_on": null,
                  "open_issues": 1,
                  "closed_issues": 0
                }
                """),
        ]);

        var sut = CreateSubject(handler);
        var milestone = new Milestone
        {
            Id = 321,
            Number = 8,
            Title = "Sprint 8",
            Description = "Updated description",
            State = "open",
        };

        // Act
        var result = await sut.UpdateMilestoneAsync("owner", "repo", 8, milestone);

        // Assert
        Assert.Equal("Updated description", result.Description);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/milestones/8", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.False(document.RootElement.TryGetProperty("due_on", out _));
    }

    [Fact]
    public async Task DeleteMilestoneAsync_ValidNumber_SendsDeleteRequest()
    {
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.NoContent),
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.DeleteMilestoneAsync("owner", "repo", 8);

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/milestones/8", handler.Requests[0].RequestUri!.ToString());
    }

    private static GitHubMilestoneRepository CreateSubject(HttpMessageHandler handler)
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

        return new GitHubMilestoneRepository(httpClientFactoryMock.Object);
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
