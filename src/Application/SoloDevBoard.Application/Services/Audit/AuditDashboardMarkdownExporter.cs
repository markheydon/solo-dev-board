using System.Globalization;
using System.Text;

namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Generates Markdown exports for audit dashboard snapshots.</summary>
public sealed class AuditDashboardMarkdownExporter : IAuditDashboardMarkdownExporter
{
    /// <inheritdoc/>
    public string GenerateSummaryMarkdown(AuditDashboardMarkdownExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new StringBuilder();
        var generatedAt = request.GeneratedAtUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        var repositoryList = string.Join(", ", request.SelectedRepositories);

        builder.AppendLine("# Audit Dashboard Summary");
        builder.AppendLine();
        builder.AppendLine($"Generated: {generatedAt}");
        builder.AppendLine($"Repositories: {repositoryList}");
        builder.AppendLine();

        var generatedAtUtc = request.GeneratedAtUtc.ToUniversalTime();

        AppendKpiSummary(builder, request);
        AppendRepositorySummary(builder, request.RepositorySummaries);
        AppendUnlabelledIssues(builder, request.UnlabelledIssues, generatedAtUtc);
        AppendStalePullRequests(builder, request.StalePullRequests, request.StalePullRequestDays, generatedAtUtc);
        AppendFailingWorkflows(builder, request.FailingWorkflowRuns, request.WorkflowHealthLoadFailed);
        AppendLabelConsistencyWarnings(builder, request.LabelConsistencyWarnings, request.LabelConsistencyLoadFailed);

        return builder.ToString().TrimEnd();
    }

    private static void AppendKpiSummary(StringBuilder builder, AuditDashboardMarkdownExportRequest request)
    {
        builder.AppendLine("## KPI summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Count |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Total open issues | {request.TotalOpenIssues} |");
        builder.AppendLine($"| Total open pull requests | {request.TotalOpenPullRequests} |");
        builder.AppendLine($"| Unlabelled issues | {request.TotalUnlabelledIssues} |");
        builder.AppendLine($"| Failing workflows | {request.TotalFailingWorkflows} |");
        builder.AppendLine($"| Label consistency warnings | {request.TotalLabelConsistencyWarnings} |");
        builder.AppendLine();
    }

    private static void AppendRepositorySummary(StringBuilder builder, IReadOnlyList<RepositoryAuditSummaryDto> repositorySummaries)
    {
        builder.AppendLine("## Repository summary");
        builder.AppendLine();

        if (repositorySummaries.Count == 0)
        {
            builder.AppendLine("No repository summary data is available.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Repository | Open issues | Open pull requests |");
        builder.AppendLine("| --- | ---: | ---: |");

        foreach (var summary in repositorySummaries)
        {
            builder.AppendLine($"| {summary.RepositoryFullName} | {summary.OpenIssueCount} | {summary.OpenPullRequestCount} |");
        }

        builder.AppendLine();
    }

    private static void AppendUnlabelledIssues(StringBuilder builder, IReadOnlyList<IssueDto> unlabelledIssues, DateTimeOffset generatedAtUtc)
    {
        builder.AppendLine("## Unlabelled issues");
        builder.AppendLine();

        if (unlabelledIssues.Count == 0)
        {
            builder.AppendLine("No unlabelled issues — great!");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Repository | Issue | Title | Age (days) |");
        builder.AppendLine("| --- | --- | --- | ---: |");

        foreach (var issue in unlabelledIssues)
        {
            var ageDays = GetDaysBetween(issue.CreatedAt, generatedAtUtc);
            builder.AppendLine($"| {issue.RepositoryFullName} | [#{issue.Number}]({issue.HtmlUrl}) | {EscapeMarkdownTableCell(issue.Title)} | {ageDays} |");
        }

        builder.AppendLine();
    }

    private static void AppendStalePullRequests(
        StringBuilder builder,
        IReadOnlyList<PullRequestDto> stalePullRequests,
        int stalePullRequestDays,
        DateTimeOffset generatedAtUtc)
    {
        builder.AppendLine($"## Stale pull requests (>{stalePullRequestDays} days)");
        builder.AppendLine();

        if (stalePullRequests.Count == 0)
        {
            builder.AppendLine("No stale pull requests — great!");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Repository | Pull request | Title | Author | Days since update |");
        builder.AppendLine("| --- | --- | --- | --- | ---: |");

        foreach (var pullRequest in stalePullRequests)
        {
            var daysSinceUpdate = GetDaysBetween(pullRequest.UpdatedAt, generatedAtUtc);
            builder.AppendLine($"| {pullRequest.RepositoryFullName} | [#{pullRequest.Number}]({pullRequest.HtmlUrl}) | {EscapeMarkdownTableCell(pullRequest.Title)} | {pullRequest.AuthorLogin} | {daysSinceUpdate} |");
        }

        builder.AppendLine();
    }

    private static void AppendFailingWorkflows(
        StringBuilder builder,
        IReadOnlyList<WorkflowRunDto> failingWorkflowRuns,
        bool workflowHealthLoadFailed)
    {
        builder.AppendLine("## Failing workflows");
        builder.AppendLine();

        if (workflowHealthLoadFailed)
        {
            builder.AppendLine("Workflow health could not be loaded for this export.");
            builder.AppendLine();
            return;
        }

        if (failingWorkflowRuns.Count == 0)
        {
            builder.AppendLine("No failing workflows — great!");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Repository | Workflow | Branch | Run |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (var workflowRun in failingWorkflowRuns)
        {
            builder.AppendLine($"| {workflowRun.RepositoryFullName} | {EscapeMarkdownTableCell(workflowRun.WorkflowName)} | {workflowRun.HeadBranch} | [Open run]({workflowRun.HtmlUrl}) |");
        }

        builder.AppendLine();
    }

    private static void AppendLabelConsistencyWarnings(
        StringBuilder builder,
        IReadOnlyList<LabelConsistencyWarningDto> warnings,
        bool labelConsistencyLoadFailed)
    {
        builder.AppendLine("## Label consistency warnings");
        builder.AppendLine();
        builder.AppendLine("Compared against the SoloDevBoard canonical taxonomy. Extra repository labels are not reported.");
        builder.AppendLine();

        if (labelConsistencyLoadFailed)
        {
            builder.AppendLine("Label consistency could not be loaded for this export.");
            builder.AppendLine();
            return;
        }

        if (warnings.Count == 0)
        {
            builder.AppendLine("Labels match the SoloDevBoard taxonomy — great!");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Repository | Label | Warning | Detail |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (var warning in warnings)
        {
            builder.AppendLine($"| {warning.RepositoryFullName} | {EscapeMarkdownTableCell(warning.LabelName)} | {FormatWarningKind(warning.Kind)} | {EscapeMarkdownTableCell(warning.Detail)} |");
        }

        builder.AppendLine();
    }

    private static string FormatWarningKind(LabelConsistencyWarningKind kind)
        => kind switch
        {
            LabelConsistencyWarningKind.Missing => "Missing",
            LabelConsistencyWarningKind.Divergent => "Divergent",
            _ => kind.ToString(),
        };

    private static int GetDaysBetween(DateTimeOffset value, DateTimeOffset referenceUtc)
    {
        var days = (int)Math.Floor((referenceUtc.ToUniversalTime() - value.ToUniversalTime()).TotalDays);
        return Math.Max(days, 0);
    }

    private static string EscapeMarkdownTableCell(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("|", "/", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
