using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for public-only docs capture mode filtering in <see cref="GitHubService"/>.</summary>
public sealed class DocsCaptureModeTests
{
    [Fact]
    public void DocsCaptureOptions_Default_IsDisabled()
    {
        var options = new DocsCaptureOptions();

        Assert.False(options.Enabled);
    }

    [Fact]
    public async Task GetRepositoriesAsync_DocsCaptureEnabled_UsesPublicTypeQueryAndExcludesPrivateRepos()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 1,
                    "name": "public-repo",
                    "full_name": "mark/public-repo",
                    "description": "Public",
                    "html_url": "https://github.com/mark/public-repo",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  },
                  {
                    "id": 2,
                    "name": "private-repo",
                    "full_name": "mark/private-repo",
                    "description": "Private",
                    "html_url": "https://github.com/mark/private-repo",
                    "private": true,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler, docsCaptureEnabled: true);

        var result = await sut.GetRepositoriesAsync(cancellationToken);

        var repository = Assert.Single(result);
        Assert.Equal("public-repo", repository.Name);
        Assert.False(repository.IsPrivate);
        Assert.Single(handler.Requests);
        Assert.Equal(
            "https://api.github.com/user/repos?sort=updated&per_page=100&type=public",
            handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_DocsCaptureDisabled_IncludesPrivateReposAndOmitsTypePublic()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 1,
                    "name": "public-repo",
                    "full_name": "mark/public-repo",
                    "description": "Public",
                    "html_url": "https://github.com/mark/public-repo",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  },
                  {
                    "id": 2,
                    "name": "private-repo",
                    "full_name": "mark/private-repo",
                    "description": "Private",
                    "html_url": "https://github.com/mark/private-repo",
                    "private": true,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler, docsCaptureEnabled: false);

        var result = await sut.GetRepositoriesAsync(cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, repository => repository.IsPrivate);
        Assert.Equal(
            "https://api.github.com/user/repos?sort=updated&per_page=100",
            handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRepositoriesAsync_OwnerScoped_DocsCaptureEnabled_ExcludesPrivateRepos()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": 1,
                    "name": "public-repo",
                    "full_name": "owner/public-repo",
                    "description": "Public",
                    "html_url": "https://github.com/owner/public-repo",
                    "private": false,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  },
                  {
                    "id": 2,
                    "name": "private-repo",
                    "full_name": "owner/private-repo",
                    "description": "Private",
                    "html_url": "https://github.com/owner/private-repo",
                    "private": true,
                    "archived": false,
                    "created_at": "2026-03-01T10:00:00Z",
                    "updated_at": "2026-03-02T11:00:00Z"
                  }
                ]
                """),
        ]);

        var sut = CreateSubject(handler, docsCaptureEnabled: true);

        var result = await sut.GetRepositoriesAsync("owner", cancellationToken);

        var repository = Assert.Single(result);
        Assert.Equal("public-repo", repository.Name);
        Assert.False(repository.IsPrivate);
    }

    [Fact]
    public async Task GetProjectBoardsForRepositoryAsync_DocsCaptureEnabled_ExcludesPrivateProjectsFromTotals()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "repository": {
                        "projectsV2": {
                            "nodes": [
                                {
                                    "id": "PVT_public",
                                    "title": "Public Roadmap",
                                    "public": true,
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
                                },
                                {
                                    "id": "PVT_private",
                                    "title": "Private Board",
                                    "public": false,
                                    "owner": { "login": "owner" },
                                    "fields": {
                                        "nodes": [
                                            {
                                                "id": "PVTF_status_private",
                                                "name": "Status",
                                                "options": [
                                                    { "id": "option-two", "name": "Todo" }
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

        var sut = CreateSubject(handler, docsCaptureEnabled: true);

        var result = await sut.GetProjectBoardsForRepositoryAsync("owner", "repo", cancellationToken);

        var board = Assert.Single(result.SupportedProjectBoards);
        Assert.Equal("Public Roadmap", board.Title);
        Assert.Equal(1, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);

        var warning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
            result.TotalLinkedProjectCount,
            result.InaccessibleLinkedProjectCount);
        Assert.Null(warning);
    }

    [Fact]
    public async Task GetBoardRulesDefinitionAsync_DocsCaptureEnabled_PrivateProject_ReturnsUnavailableFallback()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new QueueMessageHandler(
        [
            CreateJsonResponse(HttpStatusCode.OK, """
            {
                "data": {
                    "node": {
                        "id": "PVT_private",
                        "title": "Private Board",
                        "public": false,
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
                },
                "errors": []
            }
            """),
        ]);

        var sut = CreateSubject(handler, docsCaptureEnabled: true);

        var result = await sut.GetBoardRulesDefinitionAsync("owner", "repo", "PVT_private", cancellationToken);

        Assert.Equal("PVT_private", result.ProjectId);
        Assert.Empty(result.ProjectTitle);
        Assert.Contains(
            result.UnsupportedDetails,
            detail => detail.Contains("not found or is unavailable", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DocsCaptureStartupLogger_Enabled_LogsWarning()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var logger = new CapturingLogger<DocsCaptureStartupLogger>();
        var sut = new DocsCaptureStartupLogger(
            Options.Create(new DocsCaptureOptions { Enabled = true }),
            logger);

        await sut.StartAsync(cancellationToken);

        Assert.Contains(
            logger.Messages,
            message => message.Contains("Docs capture mode is enabled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DocsCaptureStartupLogger_Disabled_DoesNotLogWarning()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var logger = new CapturingLogger<DocsCaptureStartupLogger>();
        var sut = new DocsCaptureStartupLogger(
            Options.Create(new DocsCaptureOptions { Enabled = false }),
            logger);

        await sut.StartAsync(cancellationToken);

        Assert.Empty(logger.Messages);
    }

    private static GitHubService CreateSubject(HttpMessageHandler handler, bool docsCaptureEnabled)
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
            Options.Create(new DocsCaptureOptions { Enabled = docsCaptureEnabled }),
            Options.Create(new GitHubPaginationOptions()));
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

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var mediaType = request.Content.Headers.ContentType?.MediaType ?? "application/json";
                clone.Content = new StringContent(content, Encoding.UTF8, mediaType);
            }

            return clone;
        }
    }

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
