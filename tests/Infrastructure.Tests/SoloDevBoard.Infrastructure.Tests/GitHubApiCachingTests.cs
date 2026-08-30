using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Labels;
using SoloDevBoard.Infrastructure.Milestones;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for GitHub API response caching behaviour in Infrastructure.</summary>
public sealed class GitHubApiCachingTests
{
    [Fact]
    public async Task GetRepositoriesAsync_CalledTwice_UsesCacheOnSecondCall()
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
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateGitHubService(handler, memoryCache);

        // Act
        var first = await sut.GetRepositoriesAsync(cancellationToken);
        var second = await sut.GetRepositoriesAsync(cancellationToken);

        // Assert
        Assert.Single(first);
        Assert.Single(second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetLabelsAsync_CalledTwice_UsesCacheOnSecondCall()
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

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateGitHubService(handler, memoryCache);

        // Act
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetMilestonesAsync_CalledTwice_UsesCacheOnSecondCall()
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

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateGitHubService(handler, memoryCache);

        // Act
        await sut.GetMilestonesAsync("owner", "repo", cancellationToken);
        await sut.GetMilestonesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetLabelsAsync_DifferentOwnerLogins_DoNotShareCachedData()
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
                    "name": "user-a-label",
                    "color": "a2eeef",
                    "description": null
                  }
                ]
                """),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "user-b-label",
                    "color": "d73a4a",
                    "description": null
                  }
                ]
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var userAContext = GitHubCachingTestSupport.CreateCurrentUserContext("user-a");
        var userBContext = GitHubCachingTestSupport.CreateCurrentUserContext("user-b");
        var userAService = CreateGitHubService(handler, memoryCache, userAContext);
        var userBService = CreateGitHubService(handler, memoryCache, userBContext);

        // Act
        var userALabels = await userAService.GetLabelsAsync("owner", "repo", cancellationToken);
        var userBLabels = await userBService.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal("user-a-label", userALabels[0].Name);
        Assert.Equal("user-b-label", userBLabels[0].Name);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetLabelsAsync_MixedCaseOwnerAndRepo_UsesSingleCacheEntry()
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

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateGitHubService(handler, memoryCache);

        // Act
        await sut.GetLabelsAsync("Owner", "Repo", cancellationToken);
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetLabelsAsync_ReturnedCatalogueIsDefensiveCopy()
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

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateGitHubService(handler, memoryCache);

        // Act
        var first = await sut.GetLabelsAsync("owner", "repo", cancellationToken);
        var second = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.NotSame(first, second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetLabelsAsync_RepositoryAndServiceShareCacheKey_UsesSingleHttpRequest()
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

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var labelRepository = CreateLabelRepository(handler, responseCache, currentUserContext);
        var gitHubService = CreateGitHubService(handler, memoryCache, currentUserContext);

        // Act
        var repositoryLabels = await labelRepository.GetLabelsAsync("owner", "repo", cancellationToken);
        var serviceLabels = await gitHubService.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(repositoryLabels);
        Assert.Single(serviceLabels);
        Assert.Equal("repo", repositoryLabels[0].RepositoryName);
        Assert.Equal("repo", serviceLabels[0].RepositoryName);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CreateLabelAsync_AfterCachedGetLabels_RefetchesLabelsOnNextRead()
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
            CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "name": "bug",
                  "color": "d73a4a",
                  "description": "Something is not working"
                }
                """),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "enhancement",
                    "color": "a2eeef",
                    "description": null
                  },
                  {
                    "name": "bug",
                    "color": "d73a4a",
                    "description": "Something is not working"
                  }
                ]
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateLabelRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);
        await sut.CreateLabelAsync("owner", "repo", new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working" }, cancellationToken);
        var refreshed = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, refreshed.Count);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    [Fact]
    public async Task UpdateLabelAsync_AfterCachedGetLabels_RefetchesLabelsOnNextRead()
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
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "name": "enhancement",
                  "color": "1d76db",
                  "description": "Updated description"
                }
                """),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "name": "enhancement",
                    "color": "1d76db",
                    "description": "Updated description"
                  }
                ]
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateLabelRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);
        await sut.UpdateLabelAsync(
            "owner",
            "repo",
            "enhancement",
            new Label { Name = "enhancement", Colour = "1d76db", Description = "Updated description" },
            cancellationToken);
        var refreshed = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(refreshed);
        Assert.Equal("Updated description", refreshed[0].Description);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    [Fact]
    public async Task DeleteLabelAsync_AfterCachedGetLabels_RefetchesLabelsOnNextRead()
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
            new HttpResponseMessage(HttpStatusCode.NoContent),
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateLabelRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetLabelsAsync("owner", "repo", cancellationToken);
        await sut.DeleteLabelAsync("owner", "repo", "enhancement", cancellationToken);
        var refreshed = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Empty(refreshed);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Delete, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    [Fact]
    public async Task CreateMilestoneAsync_AfterCachedGetMilestones_RefetchesMilestonesOnNextRead()
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
                    "id": 123,
                    "number": 7,
                    "title": "Sprint 7",
                    "description": "Milestone description",
                    "state": "open",
                    "due_on": null,
                    "open_issues": 2,
                    "closed_issues": 5
                  }
                ]
                """),
            CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "id": 124,
                  "number": 8,
                  "title": "Sprint 8",
                  "description": "Next sprint",
                  "state": "open",
                  "due_on": null,
                  "open_issues": 0,
                  "closed_issues": 0
                }
                """),
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
                    "due_on": null,
                    "open_issues": 2,
                    "closed_issues": 5
                  },
                  {
                    "id": 124,
                    "number": 8,
                    "title": "Sprint 8",
                    "description": "Next sprint",
                    "state": "open",
                    "due_on": null,
                    "open_issues": 0,
                    "closed_issues": 0
                  }
                ]
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateMilestoneRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetMilestonesAsync("owner", "repo", cancellationToken);
        await sut.CreateMilestoneAsync(
            "owner",
            "repo",
            new Milestone
            {
                Title = "Sprint 8",
                Description = "Next sprint",
                State = "open",
            },
            cancellationToken);
        var refreshed = await sut.GetMilestonesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, refreshed.Count);
        Assert.Equal("Sprint 8", refreshed[1].Title);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    [Fact]
    public async Task UpdateMilestoneAsync_AfterCachedGetMilestones_RefetchesMilestonesOnNextRead()
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
                    "id": 123,
                    "number": 7,
                    "title": "Sprint 7",
                    "description": "Milestone description",
                    "state": "open",
                    "due_on": null,
                    "open_issues": 2,
                    "closed_issues": 5
                  }
                ]
                """),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "id": 123,
                  "number": 7,
                  "title": "Sprint 7",
                  "description": "Updated description",
                  "state": "open",
                  "due_on": null,
                  "open_issues": 2,
                  "closed_issues": 5
                }
                """),
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 123,
                    "number": 7,
                    "title": "Sprint 7",
                    "description": "Updated description",
                    "state": "open",
                    "due_on": null,
                    "open_issues": 2,
                    "closed_issues": 5
                  }
                ]
                """),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateMilestoneRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetMilestonesAsync("owner", "repo", cancellationToken);
        await sut.UpdateMilestoneAsync(
            "owner",
            "repo",
            7,
            new Milestone
            {
                Id = 123,
                Number = 7,
                Title = "Sprint 7",
                Description = "Updated description",
                State = "open",
            },
            cancellationToken);
        var refreshed = await sut.GetMilestonesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Single(refreshed);
        Assert.Equal("Updated description", refreshed[0].Description);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    [Fact]
    public async Task DeleteMilestoneAsync_AfterCachedGetMilestones_RefetchesMilestonesOnNextRead()
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
                    "id": 123,
                    "number": 7,
                    "title": "Sprint 7",
                    "description": "Milestone description",
                    "state": "open",
                    "due_on": null,
                    "open_issues": 2,
                    "closed_issues": 5
                  }
                ]
                """),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            CreateJsonResponse(HttpStatusCode.OK, "[]"),
        ]);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var currentUserContext = GitHubCachingTestSupport.CreateCurrentUserContext();
        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, currentUserContext);
        var sut = CreateMilestoneRepository(handler, responseCache, currentUserContext);

        // Act
        await sut.GetMilestonesAsync("owner", "repo", cancellationToken);
        await sut.DeleteMilestoneAsync("owner", "repo", 7, cancellationToken);
        var refreshed = await sut.GetMilestonesAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Empty(refreshed);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Delete, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
    }

    private static GitHubService CreateGitHubService(
        HttpMessageHandler handler,
        IMemoryCache memoryCache,
        ICurrentUserContext? currentUserContext = null)
    {
        var context = currentUserContext ?? GitHubCachingTestSupport.CreateCurrentUserContext();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(GitHubService.GitHubApiClientName)
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") });

        var responseCache = GitHubCachingTestSupport.CreateResponseCache(memoryCache, context);
        return new GitHubService(
            httpClientFactory,
            responseCache,
            Options.Create(new DocsCaptureOptions()),
            Options.Create(new GitHubPaginationOptions()));
    }

    private static GitHubLabelRepository CreateLabelRepository(
        HttpMessageHandler handler,
        GitHubResponseCache responseCache,
        ICurrentUserContext currentUserContext)
    {
        var authHandler = new GitHubAuthHandler(
            currentUserContext,
            Options.Create(new GitHubAuthOptions()))
        {
            InnerHandler = handler,
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(GitHubService.GitHubApiClientName)
            .Returns(client);

        return new GitHubLabelRepository(httpClientFactory, responseCache);
    }

    private static GitHubMilestoneRepository CreateMilestoneRepository(
        HttpMessageHandler handler,
        GitHubResponseCache responseCache,
        ICurrentUserContext currentUserContext)
    {
        var authHandler = new GitHubAuthHandler(
            currentUserContext,
            Options.Create(new GitHubAuthOptions()))
        {
            InnerHandler = handler,
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("https://api.github.com"),
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(GitHubService.GitHubApiClientName)
            .Returns(client);

        return new GitHubMilestoneRepository(httpClientFactory, responseCache);
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
