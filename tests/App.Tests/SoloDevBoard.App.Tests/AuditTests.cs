using System.Reflection;
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
    private readonly IAuditDashboardMarkdownExporter _markdownExporter = Substitute.For<IAuditDashboardMarkdownExporter>();
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();

    [Fact]
    public async Task Audit_WhileServiceIsLoading_ShowsFeedbackRegionSkeleton()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tcs.Task);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(CreateSnapshot());

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
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(Array.Empty<RepositoryDto>());

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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));

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
            Assert.Contains("Label consistency warnings", cut.Markup);
            Assert.Contains("audit-total-issues-card", cut.Markup);
            Assert.Contains("audit-total-prs-card", cut.Markup);

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
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);

        var snapshotCompletionSource = new TaskCompletionSource<AuditDashboardSnapshotDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(snapshotCompletionSource.Task);

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
        await _auditDashboardService.Received(1).GetDashboardSnapshotAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), 14, false, Arg.Any<CancellationToken>());

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid='audit-load-selected-button']");
            Assert.True(button.HasAttribute("disabled"));
        });

        await cut.InvokeAsync(() => snapshotCompletionSource.SetResult(CreateSnapshot([new RepositoryAuditSummaryDto("owner/repo-a", 1, 1, 0, 0, 0)])));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-summary-table']")));
    }

    [Fact]
    public async Task Audit_WhenLabelConsistencyIsLoading_ShowsKpiSkeleton()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService
            .GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns(CreateSnapshot([new RepositoryAuditSummaryDto("owner/repo-a", 1, 1, 0, 0, 0)]));

        var labelConsistencyCompletionSource = new TaskCompletionSource<IReadOnlyList<LabelConsistencyWarningDto>>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var ctx = CreateContext();
        _auditDashboardService
            .GetLabelConsistencyWarningsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(labelConsistencyCompletionSource.Task);

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-label-consistency-kpi-card'] .mud-skeleton"));
        });

        await cut.InvokeAsync(() => labelConsistencyCompletionSource.SetResult([]));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='audit-label-consistency-kpi-card'] .mud-skeleton")));
    }

    [Fact]
    public async Task Audit_WhenHealthIndicatorsExist_ShowsHealthSectionsWithCountsAndLinks()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 4, 2, 1, 1, 1),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);

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

        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary, issues, pullRequests));

        await using var ctx = CreateContext();
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
            Assert.Contains("Label Consistency", cut.Markup);
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
    public async Task Audit_WhenLabelConsistencyWarningsExist_ShowsWarningRowsAndLabelManagerLink()
    {
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 0, 0, 0, 0),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));

        await using var ctx = CreateContext();
        _auditDashboardService
            .GetLabelConsistencyWarningsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new LabelConsistencyWarningDto("owner/repo-a", "type/bug", LabelConsistencyWarningKind.Missing, "Missing from the repository."),
            ]);

        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-label-consistency-section']"));
            Assert.Single(cut.FindAll("[data-testid='audit-label-consistency-kpi-card']"));
            Assert.Contains("type/bug", cut.Markup);
            Assert.Contains("Missing from the repository.", cut.Markup);
            Assert.Contains("href=\"/labels\"", cut.Markup);
        });
    }

    [Fact]
    public async Task Audit_WhenWorkflowHealthFails_KeepsCoreAuditDataVisible()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 2, 1, 1, 0, 0),
        };

        var issues = new List<IssueDto>
        {
            new(12, "Needs triage", "https://github.com/owner/repo-a/issues/12", "owner/repo-a", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-2)),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService
            .GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns(CreateSnapshot(summary, issues));

        await using var ctx = CreateContext();
        _auditDashboardService
            .GetFailingWorkflowRunsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<WorkflowRunDto>>>(_ => throw new HttpRequestException("GitHub API request failed."));

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.Contains("Needs triage", cut.Markup);
            Assert.DoesNotContain("Unable to load audit summary", cut.Markup);
            Assert.Contains("No failing workflows — great!", cut.Markup);
            var workflowHealthLoadFailed = (bool)typeof(Audit)
                .GetField("workflowHealthLoadFailed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(cut.Instance)!;
            Assert.True(workflowHealthLoadFailed);
        });
    }

    [Fact]
    public async Task Audit_WhenLabelConsistencyFails_KeepsCoreAuditDataVisibleAndDisablesExport()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 2, 1, 1, 0, 0),
        };

        var issues = new List<IssueDto>
        {
            new(12, "Needs triage", "https://github.com/owner/repo-a/issues/12", "owner/repo-a", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-2)),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService
            .GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns(CreateSnapshot(summary, issues));

        await using var ctx = CreateContext();
        _auditDashboardService
            .GetLabelConsistencyWarningsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<LabelConsistencyWarningDto>>>(_ => throw new HttpRequestException("GitHub API request failed."));

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.Contains("Needs triage", cut.Markup);
            Assert.DoesNotContain("Unable to load audit summary", cut.Markup);
            Assert.Contains("Labels match the SoloDevBoard taxonomy — great!", cut.Markup);
            var labelConsistencyLoadFailed = (bool)typeof(Audit)
                .GetField("labelConsistencyLoadFailed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(cut.Instance)!;
            Assert.True(labelConsistencyLoadFailed);
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);

        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));

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
            Assert.Contains("Labels match the SoloDevBoard taxonomy — great!", cut.Markup);
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

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 2), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot([new RepositoryAuditSummaryDto("owner/repo-a", 4, 2, 1, 1, 0)]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        // Assert
        await _auditDashboardService.Received(1).GetDashboardSnapshotAsync(Arg.Is<IReadOnlyList<string>>(repos => repos!.Count == 1 && repos[0] == "owner/repo-a"), 14, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Audit_WhenAuditSummaryIsLoaded_ShowsAutoRefreshSelectorAndExportButton()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 1, 0, 0, 0),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));

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
            Assert.NotNull(cut.Find("[data-testid='audit-auto-refresh-select']"));
            Assert.Single(cut.FindAll("[data-testid='audit-export-markdown-button']"));
            Assert.False(cut.Find("[data-testid='audit-export-markdown-button']").HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Audit_WhenExportMarkdownIsClicked_GeneratesMarkdownAndInvokesClipboardCopy()
    {
        // Arrange
        const string markdown = "# Audit Dashboard Summary";
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 1, 0, 0, 0),
        };

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>()).Returns(CreateSnapshot(summary));
        _markdownExporter.GenerateSummaryMarkdown(Arg.Any<AuditDashboardMarkdownExportRequest>()).Returns(markdown);

        await using var ctx = CreateContext();
        var clipboardModule = ctx.JSInterop.SetupModule("./Components/Features/Audit/Pages/Audit.razor.js");
        clipboardModule.SetupVoid("copyTextToClipboard");

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-export-markdown-button']")));
        cut.Find("[data-testid='audit-export-markdown-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _markdownExporter.Received(1).GenerateSummaryMarkdown(Arg.Any<AuditDashboardMarkdownExportRequest>());
            clipboardModule.VerifyInvoke("copyTextToClipboard");
        });

        var exportRequest = _markdownExporter.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IAuditDashboardMarkdownExporter.GenerateSummaryMarkdown))
            .GetArguments()[0] as AuditDashboardMarkdownExportRequest;

        Assert.NotNull(exportRequest);
        Assert.Single(exportRequest.SelectedRepositories);
        Assert.Equal("owner/repo-a", exportRequest.SelectedRepositories[0]);
    }

    [Fact]
    public async Task Audit_WhenRendered_ShowsDefaultAutoRefreshInterval()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='audit-auto-refresh-select']"));
            Assert.Contains("Every 5 minutes", cut.Markup);
        });
    }

    [Fact]
    public async Task Audit_WhenBackgroundRefreshRuns_ShowsRefreshingIndicatorWhileDataRemainsVisible()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 1, 0, 0, 0),
        };

        var refreshCompletionSource = new TaskCompletionSource<AuditDashboardSnapshotDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshotCallCount = 0;

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                snapshotCallCount++;
                return snapshotCallCount == 1
                    ? Task.FromResult(CreateSnapshot(summary))
                    : refreshCompletionSource.Task;
            });

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-summary-table']")));

        var refreshTask = InvokeBackgroundRefreshAsync(cut);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-refreshing-state']"));
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
        });

        await cut.InvokeAsync(() => refreshCompletionSource.SetResult(CreateSnapshot(summary)));
        await refreshTask;
    }

    [Fact]
    public async Task Audit_WhenBackgroundRefreshFails_KeepsExistingDataVisible()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-a", 1, 1, 0, 0, 0),
        };

        var snapshotCallCount = 0;

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);
        _auditDashboardService.GetDashboardSnapshotAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                snapshotCallCount++;
                if (snapshotCallCount == 1)
                {
                    return Task.FromResult(CreateSnapshot(summary));
                }

                throw new HttpRequestException("rate limited");
            });

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-command-surface']")));
        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));
        cut.Find("[data-testid='audit-load-selected-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-summary-table']")));

        await InvokeBackgroundRefreshAsync(cut);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.DoesNotContain("Unable to load audit summary", cut.Markup);
        });
    }

    [Fact]
    public async Task Audit_WhenAutoRefreshIntervalChangedToOff_UpdatesSelectedInterval()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid='audit-auto-refresh-select']")));

        await InvokeAutoRefreshIntervalChangedAsync(cut, 0);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var selectedInterval = (int)typeof(Audit)
                .GetField("selectedAutoRefreshIntervalMinutes", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(cut.Instance)!;
            Assert.Equal(0, selectedInterval);
        });
    }

    private static async Task InvokeBackgroundRefreshAsync(IRenderedComponent<Audit> cut)
    {
        var method = typeof(Audit).GetMethod("LoadAuditDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        await cut.InvokeAsync(async () => await (Task)method!.Invoke(cut.Instance, [true, false])!);
    }

    private static async Task InvokeAutoRefreshIntervalChangedAsync(IRenderedComponent<Audit> cut, int intervalMinutes)
    {
        var method = typeof(Audit).GetMethod("OnAutoRefreshIntervalChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        await cut.InvokeAsync(async () => await (Task)method!.Invoke(cut.Instance, [intervalMinutes])!);
    }

    [Fact]
    public async Task Audit_AfterRepositoriesLoad_ShowsReloadFromGitHubButton()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([
                new RepositoryDto(1, "repo-a", "owner/repo-a", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], false),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Audit>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-reload-from-github-button']"));
            Assert.Contains("Reload from GitHub", cut.Markup);
        });
    }

    [Fact]
    public async Task Audit_ReloadFromGitHub_KeepsRepositorySelectionAndForceReloadsCatalogue()
    {
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns([
                repoA,
                repoB,
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Audit>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='audit-repository-autocomplete']")));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync([repoA.FullName, repoB.FullName]));

        cut.WaitForAssertion(() => Assert.Contains("2 selected", cut.Markup));

        await cut.Find("[data-testid='audit-reload-from-github-button']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("2 selected", cut.Markup));

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), true);
    }

    [Fact]
    public async Task Audit_RepositoryLoadFailure_ShowsTryAgainButton()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Connection refused")));

        await using var ctx = CreateContext();
        var cut = ctx.Render<Audit>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-reload-repositories-button']"));
            Assert.Single(cut.FindAll("[data-testid='audit-reload-from-github-button']"));
            Assert.Contains("Try again", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _auditDashboardService);
        ctx.Services.AddSingleton(_ => _markdownExporter);

        _auditDashboardService
            .GetFailingWorkflowRunsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WorkflowRunDto>());
        _auditDashboardService
            .GetLabelConsistencyWarningsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LabelConsistencyWarningDto>());

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static AuditDashboardSnapshotDto CreateSnapshot(
        IReadOnlyList<RepositoryAuditSummaryDto>? summaries = null,
        IReadOnlyList<IssueDto>? unlabelledIssues = null,
        IReadOnlyList<PullRequestDto>? stalePullRequests = null,
        IReadOnlyList<WorkflowRunDto>? failingWorkflowRuns = null)
        => new(
            summaries ?? Array.Empty<RepositoryAuditSummaryDto>(),
            unlabelledIssues ?? Array.Empty<IssueDto>(),
            stalePullRequests ?? Array.Empty<PullRequestDto>(),
            failingWorkflowRuns ?? Array.Empty<WorkflowRunDto>());

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
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Topics: [],
            IsOpenSource: false);
}
