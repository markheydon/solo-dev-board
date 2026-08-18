using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubServiceTests
{
    [Fact]
    public async Task GetRepositoriesAsync_AuthenticatedUser_UsesUserReposEndpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetRepositoriesAsync(cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetRepositoriesAsync(cancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/user/repos?sort=updated&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_MultiplePages_ReturnsMappedRepositories()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetRepositoriesAsync("owner", cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetActiveRepositoriesAsync("owner", cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetIssuesAsync("owner", "repo", cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetIssuesAsync("owner", "repo", cancellationToken);

        // Assert
        var issue = Assert.Single(result);
        Assert.Equal(32, issue.Number);
        Assert.Equal(2086799558, issue.Id);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetPullRequestsAsync_ValidResponse_ReturnsMappedPullRequests()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
                                        "labels": [
                                            { "name": "type/story", "color": "1d76db", "description": "Story" }
                                        ],
                                        "milestone": {
                                            "id": 400,
                                            "number": 7,
                                            "title": "v0.7.0",
                                            "description": "Phase 3",
                                            "state": "open",
                                            "due_on": "2026-06-01T00:00:00Z",
                                            "open_issues": 3,
                                            "closed_issues": 5
                                        },
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
        var result = await sut.GetPullRequestsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("feature-branch", result[0].HeadBranch);
        Assert.Equal("main", result[0].BaseBranch);
        Assert.True(result[0].IsDraft);
        Assert.Single(result[0].Labels);
        Assert.Equal("type/story", result[0].Labels[0].Name);
        var milestone = Assert.IsType<SoloDevBoard.Domain.Entities.Milestones.Milestone>(result[0].Milestone);
        Assert.Equal(7, milestone.Number);
        Assert.Equal("v0.7.0", milestone.Title);
        Assert.Equal("https://api.github.com/repos/owner/repo/pulls?state=all&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetIssuesAsync_OpenState_UsesOpenFilterInRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetIssuesAsync("owner", "repo", "open", cancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues?state=open&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetPullRequestsAsync_OpenState_UsesOpenFilterInRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetPullRequestsAsync("owner", "repo", "open", cancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/pulls?state=open&per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetIssuesAsync_InvalidState_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = CreateSubject(new QueueMessageHandler([]));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => _ = await sut.GetIssuesAsync("owner", "repo", "invalid", cancellationToken));
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_ValidResponse_ReturnsMappedWorkflowRuns()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetWorkflowRunsAsync("owner", "repo", cancellationToken);

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
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?per_page=30&status=completed&exclude_pull_requests=true", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_MultiplePages_ReturnsMappedWorkflowRuns()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 1,
                      "name": "build",
                      "status": "completed",
                      "conclusion": "success",
                      "head_branch": "main",
                      "head_sha": "abc123",
                      "created_at": "2026-03-10T08:00:00Z",
                      "updated_at": "2026-03-10T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/1"
                    }
                  ]
                }
                """,
                "<https://api.github.com/repos/owner/repo/actions/runs?page=2&per_page=30&status=completed&exclude_pull_requests=true>; rel=\"next\""),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 2,
                      "name": "deploy",
                      "status": "completed",
                      "conclusion": "failure",
                      "head_branch": "main",
                      "head_sha": "def456",
                      "created_at": "2026-03-11T08:00:00Z",
                      "updated_at": "2026-03-11T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/2"
                    }
                  ]
                }
                """),
        ]);

        var sut = CreateSubject(handler, new GitHubPaginationOptions { WorkflowRunsMaxPages = 5 });

        // Act
        var result = await sut.GetWorkflowRunsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("build", result[0].WorkflowName);
        Assert.Equal("deploy", result[1].WorkflowName);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?per_page=30&status=completed&exclude_pull_requests=true", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?page=2&per_page=30&status=completed&exclude_pull_requests=true", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_MaxPagesReached_StopsFetchingAdditionalPages()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 1,
                      "name": "build",
                      "status": "completed",
                      "conclusion": "success",
                      "head_branch": "main",
                      "head_sha": "abc123",
                      "created_at": "2026-03-10T08:00:00Z",
                      "updated_at": "2026-03-10T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/1"
                    }
                  ]
                }
                """,
                "<https://api.github.com/repos/owner/repo/actions/runs?page=2&per_page=30&status=completed&exclude_pull_requests=true>; rel=\"next\""),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 2,
                      "name": "deploy",
                      "status": "completed",
                      "conclusion": "failure",
                      "head_branch": "main",
                      "head_sha": "def456",
                      "created_at": "2026-03-11T08:00:00Z",
                      "updated_at": "2026-03-11T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/2"
                    }
                  ]
                }
                """,
                "<https://api.github.com/repos/owner/repo/actions/runs?page=3&per_page=100>; rel=\"next\""),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "workflow_runs": [
                    {
                      "id": 3,
                      "name": "release",
                      "status": "completed",
                      "conclusion": "success",
                      "head_branch": "main",
                      "head_sha": "ghi789",
                      "created_at": "2026-03-12T08:00:00Z",
                      "updated_at": "2026-03-12T08:05:00Z",
                      "html_url": "https://github.com/owner/repo/actions/runs/3"
                    }
                  ]
                }
                """),
        ]);

        var sut = CreateSubject(handler, new GitHubPaginationOptions { WorkflowRunsMaxPages = 2 });

        // Act
        var result = await sut.GetWorkflowRunsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("build", result[0].WorkflowName);
        Assert.Equal("deploy", result[1].WorkflowName);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_EmptyWrapper_ReturnsEmptyList()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetWorkflowRunsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/actions/runs?per_page=30&status=completed&exclude_pull_requests=true", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.BadGateway, "{\"message\":\"upstream failure\"}"),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var act = async () => _ = await sut.GetWorkflowRunsAsync("owner", "repo", cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSubject(new QueueMessageHandler([]));

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => _ = await sut.GetWorkflowRunsAsync(" ", "repo", cancellationToken));
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_RepoIsWhitespace_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSubject(new QueueMessageHandler([]));

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => _ = await sut.GetWorkflowRunsAsync("owner", " ", cancellationToken));
    }

    [Fact]
    public async Task GetMilestonesAsync_ValidResponse_ReturnsMappedMilestones()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetMilestonesAsync("owner", "repo", cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Description);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels?per_page=100", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetLabelsAsync_MojibakeDescription_RepairsDescription()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var result = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("Intentionally deferred - may be revisited later.", result[0].Description);
    }

    [Fact]
    public async Task CreateLabelAsync_ValidLabel_PostsCorrectPayload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var label = new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo" };

        // Act
        var result = await sut.CreateLabelAsync("owner", "repo", label, cancellationToken);

        // Assert
        Assert.Equal(label, result);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("bug", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("d73a4a", document.RootElement.GetProperty("color").GetString());
        Assert.Equal("Something is not working", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdateLabelAsync_ValidLabel_SendsPatchPayloadWithNewName()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var updatedLabel = new Label { Name = "enhancement", Colour = "a2eeef", Description = "Feature request", RepositoryName = "repo" };

        // Act
        var result = await sut.UpdateLabelAsync("owner", "repo", "feature", updatedLabel, cancellationToken);

        // Assert
        Assert.Equal(updatedLabel, result);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels/feature", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("enhancement", document.RootElement.GetProperty("new_name").GetString());
        Assert.Equal("a2eeef", document.RootElement.GetProperty("color").GetString());
        Assert.Equal("Feature request", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task DeleteLabelAsync_ValidLabelName_SendsDeleteRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.NoContent),
        ]);
        var sut = CreateSubject(handler);

        // Act
        await sut.DeleteLabelAsync("owner", "repo", "bug", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/labels/bug", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_ApiReturnsUnauthorised_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
          async () => _ = await sut.GetRepositoriesAsync("owner", cancellationToken));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task CreateLabelAsync_ApiReturnsBadRequest_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
            async () => _ = await sut.CreateLabelAsync("owner", "repo", label, cancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("GitHub API request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyLabelsToTriageItemAsync_ValidLabels_PutsLabelsPayload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.ApplyLabelsToTriageItemAsync("owner", "repo", 42, ["type/story", "priority/high"], cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/42/labels", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var labels = document.RootElement.GetProperty("labels");
        Assert.Equal(2, labels.GetArrayLength());
        Assert.Equal("type/story", labels[0].GetString());
        Assert.Equal("priority/high", labels[1].GetString());
    }

    [Fact]
    public async Task ApplyLabelsToTriageItemAsync_DuplicateAndWhitespaceLabels_NormalisesPayload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.ApplyLabelsToTriageItemAsync("owner", "repo", 42, [" priority/high ", "", "type/story", "priority/high"], cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var labels = document.RootElement.GetProperty("labels");
        Assert.Equal(2, labels.GetArrayLength());
        Assert.Equal("priority/high", labels[0].GetString());
        Assert.Equal("type/story", labels[1].GetString());
    }

    [Fact]
    public async Task ApplyLabelsToTriageItemAsync_ItemNumberIsNotPositive_ThrowsArgumentOutOfRangeException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSubject(new QueueMessageHandler([]));

        // Act
        var action = async () => await sut.ApplyLabelsToTriageItemAsync("owner", "repo", 0, ["type/story"], cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public async Task AssignMilestoneToTriageItemAsync_NullMilestone_SendsPatchWithNullMilestone()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.AssignMilestoneToTriageItemAsync("owner", "repo", 77, null, cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/77", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("milestone").ValueKind);
    }

    [Fact]
    public async Task AddTriageItemToProjectBoardAsync_ValidResponses_ReturnsCreatedProjectItemId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
              "node_id": "I_kwDOABCDE123"
            }
            """),
            CreateJsonResponse(HttpStatusCode.OK, """
            {
              "data": {
              "addProjectV2ItemById": {
                "item": {
                "id": "PVTI_lAHOAJefG84BQ6bhzgnrX1A"
                }
              }
              },
              "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.AddTriageItemToProjectBoardAsync("owner", "repo", 55, "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        Assert.Equal("PVTI_lAHOAJefG84BQ6bhzgnrX1A", result);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/55", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("https://api.github.com/graphql", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task AddTriageItemToProjectBoardAsync_GraphQlErrorsPresent_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
                CreateJsonResponse(HttpStatusCode.OK, """
                        {
                            "node_id": "I_kwDOABCDE123"
                        }
                        """),
                        CreateJsonResponse(HttpStatusCode.OK, """
                        {
                            "data": {
                                "addProjectV2ItemById": {
                                    "item": {
                                        "id": null
                                    }
                                }
                            },
                            "errors": [
                                {
                                    "message": "Project item could not be created"
                                }
                            ]
                        }
                        """),
                ]);

        var sut = CreateSubject(handler);

        // Act
        var action = async () => _ = await sut.AddTriageItemToProjectBoardAsync("owner", "repo", 55, "project-id", cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Contains("GitHub GraphQL request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_StatusFieldPresent_ReturnsProjectBoardOptions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "repository": {
                        "projectsV2": {
                            "nodes": [
                                {
                                    "id": "PVT_kwHOAJefG84BQ6bh",
                                    "title": "Roadmap",
                                    "owner": { "login": "owner" },
                                    "fields": {
                                        "nodes": [
                                            {
                                                "id": "PVTF_status",
                                                "name": "Status",
                                                "options": [
                                                    { "id": "option-one", "name": "In Progress" },
                                                    { "id": "option-two", "name": "Done" }
                                                ]
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardsForRepositoryAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(result.SupportedProjectBoards);
        Assert.Equal(1, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);
        Assert.Equal("Roadmap", result.SupportedProjectBoards[0].Title);
        Assert.Equal("PVTF_status", result.SupportedProjectBoards[0].StatusFieldId);
        Assert.Equal(2, result.SupportedProjectBoards[0].StatusOptions.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/graphql", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_GraphQlErrorsPresent_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "repository": null
                },
                "errors": [
                    {
                        "message": "Repository not found"
                    }
                ]
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var action = async () => _ = await sut.GetProjectBoardsForRepositoryAsync("owner", "repo", cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Contains("GitHub GraphQL request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_InaccessibleNodeWithAccessibleProject_ReturnsAccessibleProjectBoards()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "repository": {
                        "projectsV2": {
                            "nodes": [
                                null,
                                {
                                    "id": "PVT_kwHOAJefG84BGXfu",
                                    "title": "Mark's Workboard",
                                    "owner": { "login": "owner" },
                                    "fields": {
                                        "nodes": [
                                            {
                                                "id": "PVTF_status",
                                                "name": "Status",
                                                "options": [
                                                    { "id": "option-one", "name": "In Progress" }
                                                ]
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                },
                "errors": [
                    {
                        "message": "Resource not accessible by integration"
                    }
                ]
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardsForRepositoryAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(result.SupportedProjectBoards);
        Assert.Equal(2, result.TotalLinkedProjectCount);
        Assert.Equal(1, result.InaccessibleLinkedProjectCount);
        Assert.Equal("Mark's Workboard", result.SupportedProjectBoards[0].Title);
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_ProjectBoardFound_ReturnsBoardRulesDefinitionDto()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "id": "PVT_kwHOAJefG84BQ6bh",
                        "title": "Roadmap",
                        "owner": { "login": "owner" },
                        "fields": {
                            "nodes": [
                                {
                                    "id": "PVTF_status",
                                    "name": "Status",
                                    "options": [
                                        { "id": "option-one", "name": "In Progress" },
                                        { "id": "option-two", "name": "Done" }
                                    ]
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        Assert.Equal("PVT_kwHOAJefG84BQ6bh", result.ProjectId);
        Assert.Equal("Roadmap", result.ProjectTitle);
        Assert.Equal("owner", result.OwnerLogin);
        Assert.Equal(2, result.Columns.Count);
        Assert.Contains(result.UnsupportedDetails, detail => detail.Contains("not yet available", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_ProjectBoardNotFound_ReturnsUnavailableDetail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": null
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        Assert.Equal("PVT_kwHOAJefG84BQ6bh", result.ProjectId);
        Assert.Empty(result.Columns);
        Assert.Contains(result.UnsupportedDetails, detail => detail.Contains("unavailable", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_StatusFieldMissing_ReturnsStatusFieldUnsupportedDetail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "id": "PVT_kwHOAJefG84BQ6bh",
                        "title": "Roadmap",
                        "owner": { "login": "owner" },
                        "fields": {
                            "nodes": []
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        Assert.Equal("PVT_kwHOAJefG84BQ6bh", result.ProjectId);
        Assert.Empty(result.Columns);
        Assert.Contains(result.UnsupportedDetails, detail => detail.Contains("supported status field", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_GraphQlErrorsPresent_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": null
                },
                "errors": [
                    {
                        "message": "Resource not accessible by integration"
                    }
                ]
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var action = async () => _ = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Contains("GitHub GraphQL request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([]);
        var sut = CreateSubject(handler);

        // Act
        var action = async () => _ = await sut.GetBoardRulesDefinitionAsync(" ", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_StatusFieldWithEmptyOptions_ReturnsEmptyColumnsWithUnsupportedDetail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "id": "PVT_kwHOAJefG84BQ6bh",
                        "title": "Roadmap",
                        "owner": { "login": "owner" },
                        "fields": {
                            "nodes": [
                                {
                                    "id": "PVTF_status",
                                    "name": "Status",
                                    "options": []
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_kwHOAJefG84BQ6bh", cancellationToken);

        // Assert
        Assert.Empty(result.Columns);
        Assert.Empty(result.Rules);
        Assert.Contains(result.UnsupportedDetails, detail => detail.Contains("not yet available", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler([]);
        var sut = CreateSubject(handler);

        // Act
        var action = async () => _ = await sut.GetProjectBoardsForRepositoryAsync(" ", "repo", cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_BoardWithoutStatusField_IsExcludedFromSupportedBoards()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "repository": {
                        "projectsV2": {
                            "nodes": [
                                {
                                    "id": "PVT_no_status",
                                    "title": "Unsupported Board",
                                    "owner": { "login": "owner" },
                                    "fields": {
                                        "nodes": [
                                            {
                                                "id": "PVTF_priority",
                                                "name": "Priority",
                                                "options": [
                                                    { "id": "option-one", "name": "High" }
                                                ]
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardsForRepositoryAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Empty(result.SupportedProjectBoards);
        Assert.Equal(1, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);
    }

    [Fact]
    public async Task UpdateProjectBoardItemStatusAsync_ValidResponse_PostsGraphQlMutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "updateProjectV2ItemFieldValue": {
                        "projectV2Item": {
                            "id": "PVTI_lAHOAJefG84BQ6bhzgnrX1A"
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.UpdateProjectBoardItemStatusAsync("project-id", "project-item-id", "status-field-id", "in-progress", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/graphql", handler.Requests[0].RequestUri!.ToString());

        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var variables = document.RootElement.GetProperty("variables");
        Assert.Equal("project-id", variables.GetProperty("projectId").GetString());
        Assert.Equal("project-item-id", variables.GetProperty("itemId").GetString());
        Assert.Equal("status-field-id", variables.GetProperty("fieldId").GetString());
        Assert.Equal("in-progress", variables.GetProperty("statusOptionId").GetString());
    }

    [Fact]
    public async Task UpdateProjectBoardItemStatusAsync_GraphQlErrorsPresent_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "updateProjectV2ItemFieldValue": null
                },
                "errors": [
                    {
                        "message": "Field value could not be updated"
                    }
                ]
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var action = async () => await sut.UpdateProjectBoardItemStatusAsync("project-id", "project-item-id", "status-field-id", "in-progress", cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Contains("GitHub GraphQL request failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProjectBoardItemsAsync_StatusAndFocusOrderPresent_ReturnsMappedCatalogue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "fields": {
                            "nodes": [
                                { "id": "PVTF_status", "name": "Status", "dataType": "SINGLE_SELECT" },
                                { "id": "PVTF_focus", "name": "Focus Order", "dataType": "NUMBER" }
                            ]
                        },
                        "items": {
                            "pageInfo": { "hasNextPage": false, "endCursor": null },
                            "nodes": [
                                {
                                    "id": "PVTI_item-one",
                                    "updatedAt": "2026-03-01T10:00:00Z",
                                    "content": {
                                        "__typename": "Issue",
                                        "number": 40,
                                        "title": "Daily Focus",
                                        "url": "https://github.com/markheydon/solo-dev-board/issues/40",
                                        "repository": {
                                            "name": "solo-dev-board",
                                            "owner": { "login": "markheydon" }
                                        }
                                    },
                                    "status": {
                                        "optionId": "option-up-next",
                                        "name": "Up Next",
                                        "updatedAt": "2026-03-05T12:00:00Z"
                                    },
                                    "focusOrder": { "number": 2 }
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardItemsAsync("project-id", cancellationToken);

        // Assert
        Assert.Equal("PVTF_status", result.FieldIds.StatusFieldId);
        Assert.Equal("PVTF_focus", result.FieldIds.FocusOrderFieldId);
        Assert.Single(result.Items);
        Assert.Equal("PVTI_item-one", result.Items[0].ProjectItemId);
        Assert.Equal("Up Next", result.Items[0].Status?.Name);
        Assert.Equal("option-up-next", result.Items[0].Status?.OptionId);
        Assert.Equal(2, result.Items[0].FocusOrder);
        Assert.Equal(TriageItemType.Issue, result.Items[0].Content.ContentType);
        Assert.Equal(40, result.Items[0].Content.Number);
        Assert.Equal(new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero), result.Items[0].ActivityTimestamp);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetProjectBoardItemsAsync_MissingFocusOrderField_ReturnsCatalogueWithoutFocusOrderFieldId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "fields": {
                            "nodes": [
                                { "id": "PVTF_status", "name": "Status", "dataType": "SINGLE_SELECT" }
                            ]
                        },
                        "items": {
                            "pageInfo": { "hasNextPage": false, "endCursor": null },
                            "nodes": [
                                {
                                    "id": "PVTI_item-one",
                                    "updatedAt": "2026-03-01T10:00:00Z",
                                    "content": {
                                        "__typename": "PullRequest",
                                        "number": 12,
                                        "title": "PM workflow PR",
                                        "url": "https://github.com/markheydon/solo-dev-board/pull/12",
                                        "repository": {
                                            "name": "solo-dev-board",
                                            "owner": { "login": "markheydon" }
                                        }
                                    },
                                    "status": null,
                                    "focusOrder": null
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardItemsAsync("project-id", cancellationToken);

        // Assert
        Assert.Equal("PVTF_status", result.FieldIds.StatusFieldId);
        Assert.Null(result.FieldIds.FocusOrderFieldId);
        Assert.Single(result.Items);
        Assert.Null(result.Items[0].Status);
        Assert.Null(result.Items[0].FocusOrder);
        Assert.Equal(TriageItemType.PullRequest, result.Items[0].Content.ContentType);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), result.Items[0].ActivityTimestamp);
    }

    [Fact]
    public async Task GetProjectBoardItemsAsync_HasNextPage_FetchesAllPages()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "fields": {
                            "nodes": [
                                { "id": "PVTF_status", "name": "Status", "dataType": "SINGLE_SELECT" }
                            ]
                        },
                        "items": {
                            "pageInfo": { "hasNextPage": true, "endCursor": "cursor-one" },
                            "nodes": [
                                {
                                    "id": "PVTI_item-one",
                                    "updatedAt": "2026-03-01T10:00:00Z",
                                    "content": {
                                        "__typename": "Issue",
                                        "number": 1,
                                        "title": "First",
                                        "url": "https://github.com/markheydon/solo-dev-board/issues/1",
                                        "repository": {
                                            "name": "solo-dev-board",
                                            "owner": { "login": "markheydon" }
                                        }
                                    },
                                    "status": null,
                                    "focusOrder": null
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "fields": {
                            "nodes": [
                                { "id": "PVTF_status", "name": "Status", "dataType": "SINGLE_SELECT" }
                            ]
                        },
                        "items": {
                            "pageInfo": { "hasNextPage": false, "endCursor": null },
                            "nodes": [
                                {
                                    "id": "PVTI_item-two",
                                    "updatedAt": "2026-03-02T10:00:00Z",
                                    "content": {
                                        "__typename": "Issue",
                                        "number": 2,
                                        "title": "Second",
                                        "url": "https://github.com/markheydon/solo-dev-board/issues/2",
                                        "repository": {
                                            "name": "solo-dev-board",
                                            "owner": { "login": "markheydon" }
                                        }
                                    },
                                    "status": null,
                                    "focusOrder": null
                                }
                            ]
                        }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        var result = await sut.GetProjectBoardItemsAsync("project-id", cancellationToken);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("PVTI_item-one", result.Items[0].ProjectItemId);
        Assert.Equal("PVTI_item-two", result.Items[1].ProjectItemId);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task UpdateProjectBoardItemFocusOrderAsync_ValidResponse_PostsGraphQlMutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "updateProjectV2ItemFieldValue": {
                        "projectV2Item": { "id": "PVTI_item-one" }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.UpdateProjectBoardItemFocusOrderAsync("project-id", "project-item-id", "focus-field-id", 4, cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var variables = document.RootElement.GetProperty("variables");
        Assert.Equal("project-id", variables.GetProperty("projectId").GetString());
        Assert.Equal("project-item-id", variables.GetProperty("itemId").GetString());
        Assert.Equal("focus-field-id", variables.GetProperty("fieldId").GetString());
        Assert.Equal(4, variables.GetProperty("focusOrder").GetDouble());
    }

    [Fact]
    public async Task ClearProjectBoardItemFocusOrderAsync_ValidResponse_PostsGraphQlMutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "clearProjectV2ItemFieldValue": {
                        "projectV2Item": { "id": "PVTI_item-one" }
                    }
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.ClearProjectBoardItemFocusOrderAsync("project-id", "project-item-id", "focus-field-id", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
        var payload = await handler.Requests[0].Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var variables = document.RootElement.GetProperty("variables");
        Assert.Equal("project-id", variables.GetProperty("projectId").GetString());
        Assert.Equal("project-item-id", variables.GetProperty("itemId").GetString());
        Assert.Equal("focus-field-id", variables.GetProperty("fieldId").GetString());
    }

    [Fact]
    public async Task CloseTriageItemAsDuplicateAsync_Issue_PostsCommentAndClosesIssue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.Issue, 99, "#12", cancellationToken);

        // Assert
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/99/comments", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/99", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task CloseTriageItemAsDuplicateAsync_PullRequest_PostsCommentAndClosesPullRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        await sut.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.PullRequest, 100, "#22", cancellationToken);

        // Assert
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/100/comments", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("https://api.github.com/repos/owner/repo/pulls/100", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task CloseTriageItemAsDuplicateAsync_CommentRequestFails_ThrowsAndDoesNotAttemptCloseRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var handler = new QueueMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"Validation failed\"}", Encoding.UTF8, "application/json"),
            },
        ]);

        var sut = CreateSubject(handler);

        // Act
        var action = async () => await sut.CloseTriageItemAsDuplicateAsync("owner", "repo", GitHubTriageItemType.Issue, 99, "#12", cancellationToken);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/issues/99/comments", handler.Requests[0].RequestUri!.ToString());
    }

    private static GitHubService CreateSubject(HttpMessageHandler handler, GitHubPaginationOptions? paginationOptions = null)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(GitHubService.GitHubApiClientName)
            .Returns(client);

        var responseCache = GitHubCachingTestSupport.CreateResponseCache();
        return new GitHubService(
            httpClientFactory,
            responseCache,
            Options.Create(new DocsCaptureOptions()),
            Options.Create(paginationOptions ?? new GitHubPaginationOptions()));
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
