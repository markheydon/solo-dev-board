namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Configuration options for GitHub API pagination limits.</summary>
public sealed class GitHubPaginationOptions
{
    /// <summary>Configuration section name for GitHub pagination settings.</summary>
    public const string SectionName = "GitHub:Pagination";

    /// <summary>Maximum number of pages to fetch for workflow run catalogue responses.</summary>
    /// <remarks>
    /// The Audit dashboard only needs the most recent run per workflow name. GitHub returns runs
    /// newest-first, so one page is sufficient for typical solo-developer repositories.
    /// </remarks>
    public int WorkflowRunsMaxPages { get; set; } = 1;

    /// <summary>Number of workflow runs to request per page from the GitHub API.</summary>
    public int WorkflowRunsPerPage { get; set; } = 30;
}
