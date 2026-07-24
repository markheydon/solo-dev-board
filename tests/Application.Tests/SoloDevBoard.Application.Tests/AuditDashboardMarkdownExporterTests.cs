using SoloDevBoard.Application.Services.Audit;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="AuditDashboardMarkdownExporter"/>.</summary>
public sealed class AuditDashboardMarkdownExporterTests
{
    private readonly AuditDashboardMarkdownExporter _sut = new();
    private static readonly DateTimeOffset GeneratedAt = new(2026, 7, 24, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void GenerateSummaryMarkdown_WithFilteredData_IncludesKpisRepositorySummaryAndHealthSections()
    {
        // Arrange
        var request = new AuditDashboardMarkdownExportRequest(
            RepositorySummaries:
            [
                new RepositoryAuditSummaryDto("owner/repo-a", 4, 2, 1, 1, 1),
                new RepositoryAuditSummaryDto("owner/repo-b", 1, 1, 0, 0, 0),
            ],
            UnlabelledIssues:
            [
                new IssueDto(12, "Needs triage", "https://github.com/owner/repo-a/issues/12", "owner/repo-a", GeneratedAt.AddDays(-5), GeneratedAt.AddDays(-2)),
            ],
            StalePullRequests:
            [
                new PullRequestDto(44, "Update docs", "https://github.com/owner/repo-a/pull/44", "owner/repo-a", "mark", GeneratedAt.AddDays(-20)),
            ],
            FailingWorkflowRuns:
            [
                new WorkflowRunDto("build", "completed", "failure", "https://github.com/owner/repo-a/actions/runs/123", "owner/repo-a", "main"),
            ],
            SelectedRepositories: ["owner/repo-a", "owner/repo-b"],
            TotalOpenIssues: 5,
            TotalOpenPullRequests: 3,
            TotalUnlabelledIssues: 1,
            TotalFailingWorkflows: 1,
            StalePullRequestDays: 14,
            GeneratedAtUtc: GeneratedAt);

        // Act
        var markdown = _sut.GenerateSummaryMarkdown(request);

        // Assert
        Assert.Contains("# Audit Dashboard Summary", markdown);
        Assert.Contains("Generated: 2026-07-24 14:30 UTC", markdown);
        Assert.Contains("Repositories: owner/repo-a, owner/repo-b", markdown);
        Assert.Contains("| Total open issues | 5 |", markdown);
        Assert.Contains("| owner/repo-a | 4 | 2 |", markdown);
        Assert.Contains("[#12](https://github.com/owner/repo-a/issues/12)", markdown);
        Assert.Contains("| owner/repo-a | [#12](https://github.com/owner/repo-a/issues/12) | Needs triage | 5 |", markdown);
        Assert.Contains("## Stale pull requests (>14 days)", markdown);
        Assert.Contains("[#44](https://github.com/owner/repo-a/pull/44)", markdown);
        Assert.Contains("| owner/repo-a | [#44](https://github.com/owner/repo-a/pull/44) | Update docs | mark | 20 |", markdown);
        Assert.Contains("[Open run](https://github.com/owner/repo-a/actions/runs/123)", markdown);
    }

    [Fact]
    public void GenerateSummaryMarkdown_WhenHealthSectionsAreEmpty_IncludesZeroStateMessages()
    {
        // Arrange
        var request = new AuditDashboardMarkdownExportRequest(
            RepositorySummaries: [new RepositoryAuditSummaryDto("owner/repo-a", 1, 1, 0, 0, 0)],
            UnlabelledIssues: [],
            StalePullRequests: [],
            FailingWorkflowRuns: [],
            SelectedRepositories: ["owner/repo-a"],
            TotalOpenIssues: 1,
            TotalOpenPullRequests: 1,
            TotalUnlabelledIssues: 0,
            TotalFailingWorkflows: 0,
            StalePullRequestDays: 14,
            GeneratedAtUtc: GeneratedAt);

        // Act
        var markdown = _sut.GenerateSummaryMarkdown(request);

        // Assert
        Assert.Contains("No unlabelled issues — great!", markdown);
        Assert.Contains("No stale pull requests — great!", markdown);
        Assert.Contains("No failing workflows — great!", markdown);
    }

    [Fact]
    public void GenerateSummaryMarkdown_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => _sut.GenerateSummaryMarkdown(null!));

        // Assert
        Assert.Equal("request", exception.ParamName);
    }
}
