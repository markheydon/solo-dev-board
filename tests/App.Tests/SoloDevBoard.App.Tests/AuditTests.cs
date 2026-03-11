using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Shared;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Audit"/> page.</summary>
public sealed class AuditTests
{
    private readonly Mock<IAuditDashboardService> _auditDashboardServiceMock = new();
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();

    [Fact]
    public async Task Audit_WhileServiceIsLoading_ShowsLoadingSkeleton()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RepositoryAuditSummaryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        Assert.Single(cut.FindAll("[data-testid='audit-repository-filter-loading']"));
        Assert.Empty(cut.FindAll("[data-testid='audit-summary-table']"));
        Assert.Empty(cut.FindAll("[data-testid='audit-empty-state']"));
    }

    [Fact]
    public async Task Audit_WhenServiceReturnsNoRepositories_ShowsEmptyState()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RepositoryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-empty-state']"));
            Assert.Contains("No repositories found", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='audit-summary-table']"));
        });
    }

    [Fact]
    public async Task Audit_WhenServiceReturnsSummary_ShowsRowsTotalsAndRepositoryLinks()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-b", 1, 2, 0, 0, 0),
            new("owner/repo-a", 4, 3, 1, 1, 0),
        };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-filter']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a", "owner/repo-b" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.Contains("owner/repo-a", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Total open issues", cut.Markup);
            Assert.Contains("Total open pull requests", cut.Markup);
            Assert.Contains(">5<", cut.Markup);
            Assert.Contains(">5<", cut.Markup);

            var links = cut.FindAll("a")
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .ToList();

            Assert.Contains("https://github.com/owner/repo-a", links);
            Assert.Contains("https://github.com/owner/repo-b", links);
        });
    }

    [Fact]
    public async Task Audit_WhenLoadingSelectedRepositories_ShowsAuditLoadingState()
    {
        // Arrange
        var summaryCompletionSource = new TaskCompletionSource<IReadOnlyList<RepositoryAuditSummaryDto>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo-a")]);

        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(summaryCompletionSource.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-filter']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid='audit-load-selected-button']");
            Assert.False(button.HasAttribute("disabled"));
        });

        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        _auditDashboardServiceMock.Verify(
            service => service.GetAuditSummaryAsync(
                It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-loading-state']"));
        });

        await cut.InvokeAsync(() => summaryCompletionSource.SetResult([new RepositoryAuditSummaryDto("owner/repo-a", 1, 1, 0, 0, 0)]));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-summary-table']")));
    }

    [Fact]
    public async Task Audit_WhenHealthIndicatorsExist_ShowsHealthSectionsWithCountsAndLinks()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 4, 2, 1, 1, 1),
        };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo-a")]);

        var issues = new List<IssueDto>
        {
            new(12, "Needs triage", "https://github.com/owner/repo-a/issues/12", "owner/repo-a", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-2)),
        };

        var pullRequests = new List<PullRequestDto>
        {
            new(44, "Update docs", "https://github.com/owner/repo-a/pull/44", "owner/repo-a", "mark", DateTimeOffset.UtcNow.AddDays(-20)),
        };

        var workflows = new List<WorkflowRunDto>
        {
            new("build", "completed", "failure", "https://github.com/owner/repo-a/actions/runs/123", "owner/repo-a", "main"),
        };

        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        await using var ctx = CreateContext();

        _auditDashboardServiceMock
            .Setup(service => service.GetUnlabelledIssuesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(issues);
        _auditDashboardServiceMock
            .Setup(service => service.GetStalePullRequestsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pullRequests);
        _auditDashboardServiceMock
            .Setup(service => service.GetFailingWorkflowRunsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflows);

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-filter']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-health-indicator-sections']"));
            Assert.Contains("Unlabelled Issues", cut.Markup);
            Assert.Contains("Stale Pull Requests", cut.Markup);
            Assert.Contains("Failing Workflows", cut.Markup);
            Assert.Contains("Needs triage", cut.Markup);
            Assert.Contains("Update docs", cut.Markup);
            Assert.Contains("Open run", cut.Markup);

            var links = cut.FindAll("a")
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .ToList();

            Assert.Contains("https://github.com/owner/repo-a/issues/12", links);
            Assert.Contains("https://github.com/owner/repo-a/pull/44", links);
            Assert.Contains("https://github.com/owner/repo-a/actions/runs/123", links);
        });
    }

    [Fact]
    public async Task Audit_WhenHealthIndicatorsAreEmpty_ShowsZeroStateMessages()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 1, 0, 0, 0),
        };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo-a")]);

        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-filter']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No unlabelled issues — great!", cut.Markup);
            Assert.Contains("No stale pull requests — great!", cut.Markup);
            Assert.Contains("No failing workflows — great!", cut.Markup);
        });
    }

    [Fact]
    public async Task Audit_WhenRepositoryFilterChanges_LoadsFilteredHealthIndicatorData()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 4, 2, 1, 1, 0),
            new("owner/repo-b", 3, 1, 0, 0, 0),
        };

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.Is<IReadOnlyList<string>>(repos => repos.Count == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _auditDashboardServiceMock
            .Setup(service => service.GetAuditSummaryAsync(It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RepositoryAuditSummaryDto("owner/repo-a", 4, 2, 1, 1, 0)]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-filter']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        _auditDashboardServiceMock.Verify(
            service => service.GetAuditSummaryAsync(
                It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditDashboardServiceMock.Verify(
            service => service.GetUnlabelledIssuesAsync(
                It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditDashboardServiceMock.Verify(
            service => service.GetStalePullRequestsAsync(
                It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"),
                14,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditDashboardServiceMock.Verify(
            service => service.GetFailingWorkflowRunsAsync(
                It.Is<IReadOnlyList<string>>(repos => repos.Count == 1 && repos[0] == "owner/repo-a"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private BunitContext CreateContext()
    {
        _auditDashboardServiceMock
            .Setup(service => service.GetUnlabelledIssuesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IssueDto>());
        _auditDashboardServiceMock
            .Setup(service => service.GetStalePullRequestsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PullRequestDto>());
        _auditDashboardServiceMock
            .Setup(service => service.GetFailingWorkflowRunsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkflowRunDto>());

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _auditDashboardServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static RepositoryDto CreateRepository(string owner, string name)
        => new(
            Id: 0,
            Name: name,
            FullName: $"{owner}/{name}",
            Description: string.Empty,
            Url: string.Empty,
            IsPrivate: false,
            IsArchived: false,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
