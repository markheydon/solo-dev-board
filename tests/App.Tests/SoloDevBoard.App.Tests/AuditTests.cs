using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Audit.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Audit;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Audit"/> page.</summary>
public sealed class AuditTests
{
    private readonly IAuditDashboardService _auditDashboardService = Substitute.For<IAuditDashboardService>();
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();

    [Fact]
    public async Task Audit_WhileServiceIsLoading_ShowsFeedbackRegionSkeleton()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);
        _auditDashboardService.GetAuditSummaryAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<RepositoryAuditSummaryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        Assert.Single(cut.FindAll("[data-testid='audit-feedback-region']"));
        Assert.Empty(cut.FindAll("[data-testid='audit-summary-table']"));
        Assert.DoesNotContain("No repositories found", cut.Markup);
    }

    [Fact]
    public async Task Audit_WhenServiceReturnsNoRepositories_ShowsEmptyStateInFeedbackRegion()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<RepositoryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-feedback-region']"));
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardService.GetAuditSummaryAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(summary);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a", "owner/repo-b" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.Single(cut.FindAll("[data-testid='audit-kpi-summary-cards']"));
            Assert.Single(cut.FindAll("[data-testid='audit-unlabelled-kpi-card']"));
            Assert.Single(cut.FindAll("[data-testid='audit-failing-workflows-kpi-card']"));
            Assert.Contains("owner/repo-a", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Total open issues", cut.Markup);
            Assert.Contains("Total open pull requests", cut.Markup);
            Assert.Contains("Unlabelled issues", cut.Markup);
            Assert.Contains("Failing workflows", cut.Markup);
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
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo-a")]);

        var summaryCompletionSource = new TaskCompletionSource<IReadOnlyList<RepositoryAuditSummaryDto>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _auditDashboardService.GetAuditSummaryAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(summaryCompletionSource.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid='audit-load-selected-button']");
            Assert.False(button.HasAttribute("disabled"));
        });

        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        await _auditDashboardService.Received(1).GetAuditSummaryAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<CancellationToken>());

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid='audit-load-selected-button']");
            Assert.True(button.HasAttribute("disabled"));
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo-a")]);

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

        _auditDashboardService.GetAuditSummaryAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(summary);

        await using var ctx = CreateContext();

        _auditDashboardService.GetUnlabelledIssuesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(issues);
        _auditDashboardService.GetStalePullRequestsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(pullRequests);
        _auditDashboardService.GetFailingWorkflowRunsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(workflows);

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-health-indicator-sections']"));
            Assert.Single(cut.FindAll("[data-testid='audit-health-indicators-group']"));
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo-a")]);

        _auditDashboardService.GetAuditSummaryAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(summary);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardService.GetAuditSummaryAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 2), Arg.Any<CancellationToken>()).Returns(summary);
        _auditDashboardService.GetAuditSummaryAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<CancellationToken>()).Returns([new RepositoryAuditSummaryDto("owner/repo-a", 4, 2, 1, 1, 0)]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        await _auditDashboardService.Received(1).GetAuditSummaryAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<CancellationToken>());
        await _auditDashboardService.Received(1).GetUnlabelledIssuesAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<CancellationToken>());
        await _auditDashboardService.Received(1).GetStalePullRequestsAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), 14, Arg.Any<CancellationToken>());
        await _auditDashboardService.Received(1).GetFailingWorkflowRunsAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<CancellationToken>());
    }

    private BunitContext CreateContext()
    {
        _auditDashboardService.GetUnlabelledIssuesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<IssueDto>());
        _auditDashboardService.GetStalePullRequestsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<PullRequestDto>());
        _auditDashboardService.GetFailingWorkflowRunsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<WorkflowRunDto>());

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestHostedAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _auditDashboardService);

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
