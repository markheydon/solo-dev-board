using Moq;
using SoloDevBoard.Domain.Entities;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubServiceTests
{
    [Fact]
    public async Task GetRepositoriesAsync_AuthenticatedUser_UsesUserReposEndpoint()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 3,
                    "name": "repo-auth",
                    "full_name": "mark/repo-auth",
                    "description": "Authenticated repo",
                    "html_url": "https://github.com/mark/repo-auth",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetRepositoriesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("repo-auth", result[0].Name);
        Assert.False(result[0].IsArchived);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/user/repos?sort=updated&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetRepositoriesAsync();

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/user/repos?sort=updated&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_MultiplePages_ReturnsMappedRepositories()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 1,
                    "name": "repo-one",
                    "full_name": "owner/repo-one",
                    "description": "First repo",
                    "html_url": "https://github.com/owner/repo-one",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """,
                "<https://api.github.com/users/owner/repos?page=2&per_page=100>; rel=\"next\""),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 2,
                    "name": "repo-two",
                    "full_name": "owner/repo-two",
                    "description": null,
                    "html_url": "https://github.com/owner/repo-two",
                    "private": true,
                    "archived": true,
                    "created_at": "2026-03-03T10:00:00Z",
                    "updated_at": "2026-03-04T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetRepositoriesAsync("owner");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("repo-one", result[0].Name);
        Assert.Equal(string.Empty, result[1].Description);
        Assert.True(result[1].IsArchived);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.github.com/users/owner/repos?per_page=100", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.github.com/users/owner/repos?page=2&per_page=100", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_MultiplePages_ExcludesArchivedRepositories()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 1,
                    "name": "repo-one",
                    "full_name": "owner/repo-one",
                    "description": "First repo",
                    "html_url": "https://github.com/owner/repo-one",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """,
                "<https://api.github.com/users/owner/repos?page=2&per_page=100>; rel=\"next\""),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 2,
                    "name": "repo-two",
                    "full_name": "owner/repo-two",
                    "description": null,
                    "html_url": "https://github.com/owner/repo-two",
                    "private": true,
                    "archived": true,
                    "created_at": "2026-03-03T10:00:00Z",
                    "updated_at": "2026-03-04T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetActiveRepositoriesAsync("owner");

        // Assert
        Assert.Single(result);
        Assert.Equal("repo-one", result[0].Name);
        Assert.Equal("First repo", result[0].Description);
        Assert.False(result[0].IsArchived);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.github.com/users/owner/repos?per_page=100", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.github.com/users/owner/repos?page=2&per_page=100", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetIssuesAsync_ResponseContainsPullRequests_FiltersPullRequests()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 101,
                    "number": 7,
                    "title": "Bug report",
                    "body": "Details",
                    "state": "open",
                    "user": { "login": "mark" },
                    "labels": [ { "name": "bug", "color": "d73a4a", "description": "Bug" } ],
                    "milestone": {
                      "id": 11,
                      "number": 1,
                      "title": "v0.1.0",
                      "description": "Foundation",
                      "state": "open",
                      "due_on": "2026-03-30T00:00:00Z",
                      "open_issues": 1,
                      "closed_issues": 0
                    },
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  },
                  {
                    "id": 102,
                    "number": 8,
                    "title": "PR disguised as issue",
                    "body": "",
                    "state": "open",
                    "user": { "login": "mark" },
                    "labels": [],
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z",
                    "pull_request": { "url": "https://api.github.com/repos/owner/repo/pulls/8" }
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetIssuesAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal(7, result[0].Number);
        Assert.Single(result[0].Labels, label => label.Name == "bug");
        Assert.NotNull(result[0].Milestone);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues?state=all&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetIssuesAsync_ResponseContainsLargeId_MapsWithoutJsonOverflow()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 6381766854,
                    "number": 32,
                    "title": "Large id issue",
                    "body": "Details",
                    "state": "open",
                    "user": { "login": "mark" },
                    "labels": [],
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetIssuesAsync("owner", "repo");

        // Assert
        var issue = Assert.Single(result);
        Assert.Equal(32, issue.Number);
        Assert.Equal(2086799558, issue.Id);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetPullRequestsAsync_ValidResponse_ReturnsMappedPullRequests()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 200,
                    "number": 9,
                    "title": "Add feature",
                    "body": "Description",
                    "state": "open",
                    "user": { "login": "mark" },
                    "head": { "ref": "feature-branch" },
                    "base": { "ref": "main" },
                    "draft": true,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetPullRequestsAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal("feature-branch", result[0].HeadBranch);
        Assert.Equal("main", result[0].BaseBranch);
        Assert.True(result[0].IsDraft);
        Assert.Equal("https://api.github.com/repos/owner/repo/pulls?state=all&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_ValidResponse_ReturnsMappedWorkflowRuns()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 12345,
                      "name": ".NET CI",
                      "status": "completed",
                      "conclusion": "success",
                      "head_branch": "main",
                      "head_sha": "abc123",
                      "created_at": "2026-03-10T08:00:00Z",
                      "updated_at": "2026-03-10T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/12345"
                    }
                  ]
                }
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetWorkflowRunsAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal(12345, result[0].Id);
        Assert.Equal(".NET CI", result[0].WorkflowName);
        Assert.Equal("completed", result[0].Status);
        Assert.Equal("success", result[0].Conclusion);
        Assert.Equal("main", result[0].HeadBranch);
        Assert.Equal("abc123", result[0].HeadSha);
        Assert.Equal("https://github.com/owner/repo/actions/runs/12345", result[0].HtmlUrl);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?per_page=25", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_EmptyWrapper_ReturnsEmptyList()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
                {
                  "workflow_runs": []
                }
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetWorkflowRunsAsync("owner", "repo");

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?per_page=25", handler.Requests[0].RequestUri!.ToString());
    }

      [Fact]
      public async Task GetWorkflowRunsAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
      {
        // Arrange
        var handler = new QueueMessageHandler(
        [
          CreateJsonResponse(HttpStatusCode.BadGateway, "{\"message\":\"upstream failure\"}"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var act = async () => _ = await sut.GetWorkflowRunsAsync("owner", "repo");

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("status code 502", exception.Message, StringComparison.OrdinalIgnoreCase);
      }

    [Fact]
    public async Task GetWorkflowRunsAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSubject(new QueueMessageHandler([]));

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => _ = await sut.GetWorkflowRunsAsync(" ", "repo"));
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_RepoIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSubject(new QueueMessageHandler([]));

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => _ = await sut.GetWorkflowRunsAsync("owner", " "));
    }

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
                    "id": 300,
                    "number": 10,
                    "title": "v0.2.0",
                    "description": null,
                    "state": "open",
                    "due_on": null,
                    "open_issues": 3,
                    "closed_issues": 1
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetMilestonesAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Description);
        Assert.Equal(3, result[0].OpenIssues);
        Assert.Equal(1, result[0].ClosedIssues);
        Assert.Equal("https://api.github.com/repos/owner/repo/milestones?state=all&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

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
        Assert.Equal(string.Empty, result[0].Description);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels?per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetLabelsAsync_MojibakeDescription_RepairsDescription()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "out-of-scope",
                    "color": "d4c5f9",
                    "description": "Intentionally deferred \u00d4\u00c7\u00f6 may be revisited later."
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetLabelsAsync("owner", "repo");

        // Assert
        Assert.Single(result);
        Assert.Equal("Intentionally deferred - may be revisited later.", result[0].Description);
    }

    [Fact]
    public async Task CreateLabelAsync_ValidLabel_PostsCorrectPayload()
    {
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.Created, """
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
        Assert.Equal(label, result);
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
            CreateJsonResponse(HttpStatusCode.OK, """
                {
                  "name": "enhancement",
                  "color": "a2eeef",
                  "description": "Feature request"
                }
                """),
        ]);

        var sut = CreateSubject(handler);
        var updatedLabel = new Label { Name = "enhancement", Colour = "a2eeef", Description = "Feature request" };

        // Act
        var result = await sut.UpdateLabelAsync("owner", "repo", "feature", updatedLabel);

        // Assert
        Assert.Equal(updatedLabel, result);
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
    public async Task GetRepositoriesAsync_ApiReturnsUnauthorised_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"message\":\"Bad credentials\"}", Encoding.UTF8, "application/json"),
            },
        ]);
        var sut = CreateSubject(handler);

        // Act / Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
          async () => _ = await sut.GetRepositoriesAsync("owner"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
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
        var label = new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working" };

        // Act / Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => _ = await sut.CreateLabelAsync("owner", "repo", label));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("GitHub API request failed", exception.Message, StringComparison.Ordinal);
    }

    private static GitHubService CreateSubject(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(GitHubService.GitHubApiClientName))
            .Returns(client);

        return new GitHubService(httpClientFactoryMock.Object);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json, string? linkHeader = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(linkHeader))
        {
            response.Headers.TryAddWithoutValidation("Link", linkHeader);
        }

        return response;
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
